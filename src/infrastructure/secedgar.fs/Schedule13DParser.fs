namespace secedgar.fs

open System
open System.IO
open System.Text.RegularExpressions
open System.Xml
open System.Xml.Linq
open Microsoft.Extensions.Logging

// Used for testing: https://www.sec.gov/Archives/edgar/data/1361570/000136157026000003/primary_doc.xml
// Full filing index: https://www.sec.gov/Archives/edgar/data/1707753/000136157026000003/0001361570-26-000003-index.html
// Issuer: Elastic N.V. (ESTC), Filer: Pictet Asset Management SA

module Schedule13DParser =
    open System.Globalization
    
    let private xname name = XName.Get(name)
    let private xnameWithNs ns name = XName.Get(name, ns)
    
    /// Create secure XmlReader to prevent XXE attacks
    let private createSecureXmlReader (xml: string) =
        let settings = XmlReaderSettings()
        settings.DtdProcessing <- DtdProcessing.Prohibit
        settings.XmlResolver <- null
        settings.CloseInput <- true
        let stringReader = new StringReader(xml)
        XmlReader.Create(stringReader, settings)
    
    /// Helper to get XName with optional namespace
    let private getXName (ns: string option) (name: string) =
        match ns with
        | Some nsUri -> xnameWithNs nsUri name
        | None -> xname name
    
    /// Try to get element value, handling missing elements and namespace
    let private tryGetElementValue (element: XElement) (ns: string option) (name: string) =
        let el = element.Element(getXName ns name)
        if el <> null && not (String.IsNullOrWhiteSpace(el.Value)) then
            Some (el.Value.Trim())
        else
            None
    
    /// Try to get first descendant element with optional namespace
    let private tryGetDescendant (element: XElement) (ns: string option) (name: string) =
        element.Descendants(getXName ns name) |> Seq.tryHead
    
    /// Try to parse int64 from string, handling commas, apostrophes, whitespace, and decimals.
    /// Schedule 13D filings sometimes use ' as thousands separator (e.g. Swiss filers: 5'288'262.00)
    let private tryParseInt64 (value: string option) =
        match value with
        | None -> None
        | Some v ->
            let cleaned = v.Trim().Replace(",", "").Replace("'", "").Replace(" ", "")
            match Decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture) with
            | (true, num) -> Some (int64 (Math.Round(num, 0, MidpointRounding.AwayFromZero)))
            | _ -> None
    
    /// Try to parse decimal from string, handling commas and whitespace
    let private tryParseDecimal (value: string option) =
        match value with
        | None -> None
        | Some v ->
            let cleaned = v.Trim().Replace(",", "").Replace(" ", "")
            match Decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture) with
            | true, num -> Some num
            | _ -> None
    
    /// Try to parse DateTimeOffset - handles both yyyy-MM-dd and MM/dd/yyyy formats
    /// (Schedule 13D uses MM/dd/yyyy for dateOfEvent and signature dates)
    let private tryParseDate (value: string option) =
        match value with
        | None -> None
        | Some v ->
            let formats = [| "MM/dd/yyyy"; "yyyy-MM-dd"; "M/d/yyyy"; "M/dd/yyyy"; "MM/d/yyyy" |]
            match DateTimeOffset.TryParseExact(v, formats, null, DateTimeStyles.None) with
            | (true, date) -> Some date
            | _ -> None
    
    /// Extract numbers from narrative text (handles , and ' thousands separators)
    /// Returns the number as a string after cleaning separators
    let private extractNumbersFromText (text: string) =
        // Match patterns like: 5'288'262.00, 5,274,370, 5288262
        let pattern = @"\b(\d{1,3}(?:[',]\d{3})*(?:\.\d+)?|\d+(?:\.\d+)?)\b"
        Regex.Matches(text, pattern)
        |> Seq.cast<Match>
        |> Seq.map (fun m -> m.Groups.[1].Value.Replace("'", "").Replace(",", ""))
        |> Seq.choose (fun s ->
            match Decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture) with
            | true, n -> Some n
            | _ -> None)
        |> Seq.toList
    
    /// Extract total shares from item 5 narrative text.
    /// Looks for the largest number appearing before the word "shares" in the text.
    let private extractSharesFromNarrative (text: string) =
        // Pattern: number (with possible separators) followed by whitespace and "shares"
        let pattern = @"(\d{1,3}(?:[',]\d{3})*(?:\.\d+)?|\d+(?:\.\d+)?)\s+shares?"
        let matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase)
        if matches.Count = 0 then None
        else
            matches
            |> Seq.cast<Match>
            |> Seq.choose (fun m ->
                let cleaned = m.Groups.[1].Value.Replace("'", "").Replace(",", "")
                match Decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture) with
                | true, n -> Some n
                | _ -> None)
            |> Seq.sortDescending
            |> Seq.tryHead
            |> Option.map (fun n -> int64 (Math.Round(n, 0, MidpointRounding.AwayFromZero)))
    
    /// Extract percentage from item 5 narrative text.
    /// Looks for patterns like "5.02%" or "5.02 percent".
    let private extractPercentFromNarrative (text: string) =
        let pattern = @"(\d+(?:[.,]\d+)?)\s*(?:%|percent)"
        let m = Regex.Match(text, pattern, RegexOptions.IgnoreCase)
        if m.Success then
            let numStr = m.Groups.[1].Value.Replace(",", ".")
            match Decimal.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture) with
            | true, n -> Some n
            | _ -> None
        else None
    
    /// Extract sole voting power from item 5 narrative text.
    /// Looks for "Sole power to vote on X shares" or "sole voting power: X".
    let private extractSoleVotingFromNarrative (text: string) =
        let patterns = [
            @"[Ss]ole\s+power\s+to\s+vote\s+(?:on\s+)?(\d{1,3}(?:[',]\d{3})*(?:\.\d+)?)"
            @"[Ss]ole\s+[Vv]oting\s+[Pp]ower[:\s]+(\d{1,3}(?:[',]\d{3})*(?:\.\d+)?)"
        ]
        patterns
        |> List.tryPick (fun pattern ->
            let m = Regex.Match(text, pattern)
            if m.Success then
                let cleaned = m.Groups.[1].Value.Replace("'", "").Replace(",", "")
                match Decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture) with
                | true, n -> Some (int64 (Math.Round(n, 0, MidpointRounding.AwayFromZero)))
                | _ -> None
            else None)
    
    /// Extract shared voting power from item 5 narrative text.
    let private extractSharedVotingFromNarrative (text: string) =
        let patterns = [
            @"[Ss]hared\s+power\s+to\s+vote\s+(?:on\s+)?(\d{1,3}(?:[',]\d{3})*(?:\.\d+)?)"
            @"[Ss]hared\s+[Vv]oting\s+[Pp]ower[:\s]+(\d{1,3}(?:[',]\d{3})*(?:\.\d+)?)"
        ]
        patterns
        |> List.tryPick (fun pattern ->
            let m = Regex.Match(text, pattern)
            if m.Success then
                let cleaned = m.Groups.[1].Value.Replace("'", "").Replace(",", "")
                match Decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture) with
                | true, n -> Some (int64 (Math.Round(n, 0, MidpointRounding.AwayFromZero)))
                | _ -> None
            else None)
    
    /// Extract sole dispositive power from item 5 narrative text.
    let private extractSoleDispositivFromNarrative (text: string) =
        let patterns = [
            @"[Ss]ole\s+(?:power\s+of\s+)?[Dd]ispositive?\s+[Pp]ower[:\s]+(\d{1,3}(?:[',]\d{3})*(?:\.\d+)?)"
            @"[Ss]ole\s+[Pp]ower\s+to\s+[Dd]ispose\s+(?:of\s+)?(\d{1,3}(?:[',]\d{3})*(?:\.\d+)?)"
        ]
        patterns
        |> List.tryPick (fun pattern ->
            let m = Regex.Match(text, pattern)
            if m.Success then
                let cleaned = m.Groups.[1].Value.Replace("'", "").Replace(",", "")
                match Decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture) with
                | true, n -> Some (int64 (Math.Round(n, 0, MidpointRounding.AwayFromZero)))
                | _ -> None
            else None)
    
    /// Extract shared dispositive power from item 5 narrative text.
    let private extractSharedDispositivFromNarrative (text: string) =
        let patterns = [
            @"[Ss]hared\s+(?:power\s+of\s+)?[Dd]ispositive?\s+[Pp]ower[:\s]+(\d{1,3}(?:[',]\d{3})*(?:\.\d+)?)"
            @"[Ss]hared\s+[Pp]ower\s+to\s+[Dd]ispose\s+(?:of\s+)?(\d{1,3}(?:[',]\d{3})*(?:\.\d+)?)"
        ]
        patterns
        |> List.tryPick (fun pattern ->
            let m = Regex.Match(text, pattern)
            if m.Success then
                let cleaned = m.Groups.[1].Value.Replace("'", "").Replace(",", "")
                match Decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture) with
                | true, n -> Some (int64 (Math.Round(n, 0, MidpointRounding.AwayFromZero)))
                | _ -> None
            else None)
    
    /// Parse Schedule 13D from XML document (handles real SEC format with narrative item fields)
    let parseXml (xml: string) (logger: ILogger option) =
        try
            use reader = createSecureXmlReader xml
            let doc = XDocument.Load(reader)
            let root = doc.Root
            
            let mutable notes = []
            let mutable parsed = Schedule13DHelpers.empty
            
            // Set raw XML for debugging
            parsed <- { parsed with RawXml = Some xml }
            
            // Try to detect namespace (SEC uses http://www.sec.gov/edgar/schedule13D)
            let ns = 
                if root.Name.NamespaceName <> "" then 
                    Some root.Name.NamespaceName
                else 
                    None
            
            logger |> Option.iter (fun l -> 
                l.LogDebug("Parsing Schedule 13D XML, namespace: {namespace}", defaultArg ns "none"))
            
            // --- headerData ---
            let headerData = tryGetDescendant root ns "headerData"
            
            match headerData with
            | Some header ->
                let submissionType = tryGetElementValue header ns "submissionType"
                parsed <- { parsed with IsAmendment = submissionType |> Option.exists (fun s -> s.Contains("/A")) }
                
                let filerInfo = tryGetDescendant header ns "filerInfo"
                match filerInfo with
                | Some info ->
                    let creds = tryGetDescendant info ns "filerCredentials"
                    match creds with
                    | Some c ->
                        let cik = tryGetElementValue c ns "cik"
                        parsed <- { parsed with FilerCik = cik }
                        logger |> Option.iter (fun l -> l.LogDebug("Found filer CIK: {cik}", cik))
                    | None ->
                        notes <- "Could not find filerCredentials element" :: notes
                | None ->
                    notes <- "Could not find filerInfo element" :: notes
            | None ->
                notes <- "Could not find headerData element" :: notes
            
            // --- formData ---
            let formData = tryGetDescendant root ns "formData"
            
            match formData with
            | Some form ->
                
                // --- coverPageHeader ---
                let coverPageHeader = tryGetDescendant form ns "coverPageHeader"
                match coverPageHeader with
                | Some header ->
                    // Securities class title
                    let secClass = tryGetElementValue header ns "securitiesClassTitle"
                    parsed <- { parsed with SecuritiesClassTitle = secClass }
                    
                    // Date of event (MM/dd/yyyy format in 13D unlike 13G's yyyy-MM-dd)
                    let dateOfEvent = tryGetElementValue header ns "dateOfEvent"
                    parsed <- { parsed with AsOfDate = tryParseDate dateOfEvent }
                    
                    if dateOfEvent.IsSome && parsed.AsOfDate.IsNone then
                        notes <- $"Could not parse dateOfEvent: '{dateOfEvent.Value}'" :: notes
                    
                    // Issuer info
                    let issuerInfo = tryGetDescendant header ns "issuerInfo"
                    match issuerInfo with
                    | Some issuer ->
                        let issuerName = tryGetElementValue issuer ns "issuerName"
                        let issuerCik  = tryGetElementValue issuer ns "issuerCIK"
                        let issuerCusip = tryGetElementValue issuer ns "issuerCUSIP"
                        parsed <- { parsed with 
                                      IssuerName = defaultArg issuerName ""
                                      IssuerCik  = issuerCik
                                      IssuerCusip = issuerCusip }
                        logger |> Option.iter (fun l -> 
                            l.LogDebug("Found issuer: {name} (CIK: {cik})", parsed.IssuerName, issuerCik))
                    | None ->
                        notes <- "Could not find issuerInfo element" :: notes
                | None ->
                    notes <- "Could not find coverPageHeader element" :: notes
                
                // --- item2: citizenship / background ---
                let item2 = tryGetDescendant form ns "item2"
                match item2 with
                | Some i2 ->
                    let citizenship = tryGetElementValue i2 ns "citizenship"
                    parsed <- { parsed with Citizenship = citizenship }
                | None -> ()  // item2 is optional; don't add a note
                
                // --- item4: purpose of acquisition ---
                let item4 = tryGetDescendant form ns "item4"
                match item4 with
                | Some i4 ->
                    let purpose = tryGetElementValue i4 ns "transactionPurpose"
                    if purpose.IsSome then
                        // Trim to reasonable length and store
                        let trimmed = 
                            purpose.Value
                            |> fun s -> if s.Length > 1000 then s.[..999] + "..." else s
                        parsed <- { parsed with AcquisitionPurpose = Some trimmed }
                | None -> ()  // item4 is optional
                
                // --- item5: shares, percentage, voting powers (narrative text) ---
                let item5 = tryGetDescendant form ns "item5"
                match item5 with
                | Some i5 ->
                    // Extract shares & percent from percentageOfClassSecurities text
                    let pctSecText = tryGetElementValue i5 ns "percentageOfClassSecurities"
                    match pctSecText with
                    | Some text ->
                        let shares = extractSharesFromNarrative text
                        let percent = extractPercentFromNarrative text
                        
                        parsed <- { parsed with
                                      SharesOwned = defaultArg shares 0L
                                      PercentOfClass = defaultArg percent 0.0m }
                        
                        if shares.IsNone then
                            notes <- "Could not extract shares from item5/percentageOfClassSecurities text" :: notes
                        if percent.IsNone then
                            notes <- "Could not extract percent from item5/percentageOfClassSecurities text" :: notes
                        
                        logger |> Option.iter (fun l ->
                            l.LogDebug("Extracted from item5 text: {shares} shares, {pct}%", 
                                       parsed.SharesOwned, parsed.PercentOfClass))
                    | None ->
                        notes <- "Could not find item5/percentageOfClassSecurities element" :: notes
                    
                    // Extract voting powers from numberOfShares narrative text
                    let numSharesText = tryGetElementValue i5 ns "numberOfShares"
                    match numSharesText with
                    | Some text ->
                        let soleVoting    = extractSoleVotingFromNarrative text
                        let sharedVoting  = extractSharedVotingFromNarrative text
                        let soleDisp      = extractSoleDispositivFromNarrative text
                        let sharedDisp    = extractSharedDispositivFromNarrative text
                        parsed <- { parsed with
                                      SoleVotingPower    = soleVoting
                                      SharedVotingPower  = sharedVoting
                                      SoleDispositivePower  = soleDisp
                                      SharedDispositivePower = sharedDisp }
                    | None -> ()  // voting detail is optional in 13D
                | None ->
                    notes <- "Could not find item5 element" :: notes
                
                // --- signatureInfo: filer name & filing date ---
                let sigInfo = tryGetDescendant form ns "signatureInfo"
                match sigInfo with
                | Some sigI ->
                    let sigPerson = tryGetDescendant sigI ns "signaturePerson"
                    match sigPerson with
                    | Some sp ->
                        // Reporting person name (most reliably structured field for 13D)
                        let reportingPerson = tryGetElementValue sp ns "signatureReportingPerson"
                        parsed <- { parsed with FilerName = defaultArg reportingPerson "" }
                        
                        let sigDetails = tryGetDescendant sp ns "signatureDetails"
                        match sigDetails with
                        | Some details ->
                            let dateStr = tryGetElementValue details ns "date"
                            parsed <- { parsed with FilingDate = defaultArg (tryParseDate dateStr) DateTimeOffset.UtcNow }
                        | None ->
                            notes <- "Could not find signatureDetails element" :: notes
                    | None ->
                        notes <- "Could not find signaturePerson element" :: notes
                | None ->
                    notes <- "Could not find signatureInfo element" :: notes
            | None ->
                notes <- "Could not find formData element" :: notes
            
            // Infer entity type (13D has no structured entity type field)
            parsed <- { parsed with EntityType = Schedule13DHelpers.inferEntityType parsed.Citizenship }
            
            // Calculate confidence and attach notes
            let confidence = Schedule13DHelpers.calculateConfidence parsed
            let parsingNotes = List.rev notes
            let parsedWithMeta = 
                { parsed with 
                    Confidence = confidence
                    ParsingNotes = parsingNotes }
            
            logger |> Option.iter (fun l -> 
                l.LogInformation("Parsed Schedule 13D with confidence {confidence:F2}", confidence))
            
            // Determine result type based on confidence
            if confidence >= 0.7 && notes.Length = 0 then
                Schedule13DSuccess parsedWithMeta
            elif confidence >= 0.5 then
                Schedule13DPartialSuccess (parsedWithMeta, parsingNotes)
            else
                let notesStr = String.Join("; ", parsingNotes)
                Schedule13DFailure $"Low confidence parse ({confidence:F2}). Notes: {notesStr}"
            
        with ex ->
            logger |> Option.iter (fun l -> 
                l.LogError(ex, "Error parsing Schedule 13D XML"))
            Schedule13DFailure $"XML parsing error: {ex.Message}"
    
    /// Parse from XML file path
    let parseFromFile (logger: ILogger option) (filePath: string) =
        try
            let xml = File.ReadAllText(filePath)
            parseXml xml logger
        with ex ->
            logger |> Option.iter (fun l -> 
                l.LogError(ex, "Error reading Schedule 13D file: {path}", filePath))
            Schedule13DFailure $"File read error: {ex.Message}"
    
    /// Parse from XML string content
    let parseFromDocument (logger: ILogger option) (xml: string) =
        try
            logger |> Option.iter (fun l -> 
                l.LogDebug "Parsing Schedule 13D XML document")
            parseXml xml logger
        with ex ->
            logger |> Option.iter (fun l -> 
                l.LogError(ex, "Error parsing Schedule 13D XML document"))
            Schedule13DFailure $"XML parsing error: {ex.Message}"

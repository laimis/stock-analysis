namespace secedgar.fs

open System
open System.IO
open System.Net.Http
open System.Xml.Linq
open Microsoft.Extensions.Logging

// used this for testing https://www.sec.gov/Archives/edgar/data/315066/000031506626000439/primary_doc.xml
// human friendly: https://www.sec.gov/Archives/edgar/data/1516513/000031506626000439/xslSCHEDULE_13G_X01/primary_doc.xml
// TODO: Implement a mechanism to retrieve XML versions for all relevant SEC Schedule 13G documents.

module Schedule13GParser =
    
    let private xname name = XName.Get(name)
    let private xnameWithNs ns name = XName.Get(name, ns)
    
    /// Try to get element value, handling missing elements
    let private tryGetElementValue (element: XElement) (name: string) =
        let el = element.Element(xname name)
        if el <> null && not (String.IsNullOrWhiteSpace(el.Value)) then
            Some el.Value
        else
            None
    
    /// Try to get element value with namespace
    let private tryGetElementValueNs (element: XElement) (ns: string) (name: string) =
        let el = element.Element(xnameWithNs ns name)
        if el <> null && not (String.IsNullOrWhiteSpace(el.Value)) then
            Some el.Value
        else
            None
    
    /// Try to parse int64 from string, handling commas, whitespace, and decimals
    let private tryParseInt64 (value: string option) =
        match value with
        | None -> None
        | Some v ->
            let cleaned = v.Trim().Replace(",", "").Replace(" ", "")
            // Try parsing as decimal first to handle fractional shares, then round
            match Decimal.TryParse(cleaned) with
            | (true, num) -> Some (int64 (Math.Round(num, 0, MidpointRounding.AwayFromZero)))
            | _ -> None
    
    /// Try to parse decimal from string, handling commas and whitespace
    let private tryParseDecimal (value: string option) =
        match value with
        | None -> None
        | Some v ->
            let cleaned = v.Trim().Replace(",", "").Replace(" ", "")
            match Decimal.TryParse(cleaned) with
            | (true, num) -> Some num
            | _ -> None
    
    /// Try to parse DateTimeOffset
    let private tryParseDate (value: string option) =
        match value with
        | None -> None
        | Some v ->
            match DateTimeOffset.TryParse(v) with
            | (true, date) -> Some date
            | _ -> None
    
    /// Parse Schedule 13G from XML document (handles real SEC format)
    let parseXml (xml: string) (logger: ILogger option) =
        try
            let doc = XDocument.Parse(xml)
            let root = doc.Root
            
            let mutable notes = []
            let mutable parsed = Schedule13GHelpers.empty
            
            // Set raw XML for debugging
            parsed <- { parsed with RawXml = Some xml }
            
            // Try to detect namespace (SEC uses http://www.sec.gov/edgar/schedule13g)
            let ns = 
                if root.Name.NamespaceName <> "" then 
                    Some root.Name.NamespaceName
                else 
                    None
            
            logger |> Option.iter (fun l -> 
                l.LogDebug("Parsing Schedule 13G XML, namespace: {namespace}", defaultArg ns "none"))
            
            // Parse headerData section
            let headerData = 
                match ns with
                | Some nsUri -> root.Descendants(xnameWithNs nsUri "headerData") |> Seq.tryHead
                | None -> root.Descendants(xname "headerData") |> Seq.tryHead
            
            match headerData with
            | Some header ->
                // Get submission type (SCHEDULE 13G or SCHEDULE 13G/A)
                let submissionType = 
                    match ns with
                    | Some nsUri -> tryGetElementValueNs header nsUri "submissionType"
                    | None -> tryGetElementValue header "submissionType"
                
                parsed <- { parsed with IsAmendment = submissionType |> Option.exists (fun s -> s.Contains("/A")) }
                
                // Get filer CIK from headerData
                let filerInfo = 
                    match ns with
                    | Some nsUri -> header.Descendants(xnameWithNs nsUri "filerInfo") |> Seq.tryHead
                    | None -> header.Descendants(xname "filerInfo") |> Seq.tryHead
                
                match filerInfo with
                | Some info ->
                    let creds = 
                        match ns with
                        | Some nsUri -> info.Descendants(xnameWithNs nsUri "filerCredentials") |> Seq.tryHead
                        | None -> info.Descendants(xname "filerCredentials") |> Seq.tryHead
                    
                    match creds with
                    | Some c ->
                        let cik = 
                            match ns with
                            | Some nsUri -> tryGetElementValueNs c nsUri "cik"
                            | None -> tryGetElementValue c "cik"
                        
                        parsed <- { parsed with FilerCik = cik }
                        
                        logger |> Option.iter (fun l -> 
                            l.LogDebug("Found filer CIK: {cik}", cik))
                    | None ->
                        notes <- "Could not find filerCredentials element" :: notes
                | None ->
                    notes <- "Could not find filerInfo element" :: notes
            | None ->
                notes <- "Could not find headerData element" :: notes
            
            // Parse formData section (main content)
            let formData = 
                match ns with
                | Some nsUri -> root.Descendants(xnameWithNs nsUri "formData") |> Seq.tryHead
                | None -> root.Descendants(xname "formData") |> Seq.tryHead
            
            match formData with
            | Some form ->
                // Parse coverPageHeader (issuer info, dates)
                let coverPageHeader = 
                    match ns with
                    | Some nsUri -> form.Descendants(xnameWithNs nsUri "coverPageHeader") |> Seq.tryHead
                    | None -> form.Descendants(xname "coverPageHeader") |> Seq.tryHead
                
                match coverPageHeader with
                | Some header ->
                    // Get issuer information
                    let issuerInfo = 
                        match ns with
                        | Some nsUri -> header.Descendants(xnameWithNs nsUri "issuerInfo") |> Seq.tryHead
                        | None -> header.Descendants(xname "issuerInfo") |> Seq.tryHead
                    
                    match issuerInfo with
                    | Some issuer ->
                        let issuerName = 
                            match ns with
                            | Some nsUri -> tryGetElementValueNs issuer nsUri "issuerName"
                            | None -> tryGetElementValue issuer "issuerName"
                        
                        let issuerCik = 
                            match ns with
                            | Some nsUri -> tryGetElementValueNs issuer nsUri "issuerCik"
                            | None -> tryGetElementValue issuer "issuerCik"
                        
                        parsed <- { parsed with 
                                      IssuerName = defaultArg issuerName ""
                                      IssuerCik = issuerCik }
                        
                        logger |> Option.iter (fun l -> 
                            l.LogDebug("Found issuer: {name} (CIK: {cik})", parsed.IssuerName, issuerCik))
                    | None ->
                        notes <- "Could not find issuerInfo element" :: notes
                    
                    // Get event date (as-of date)
                    let eventDate = 
                        match ns with
                        | Some nsUri -> tryGetElementValueNs header nsUri "eventDateRequiresFilingThisStatement"
                        | None -> tryGetElementValue header "eventDateRequiresFilingThisStatement"
                    
                    parsed <- { parsed with AsOfDate = tryParseDate eventDate }
                | None ->
                    notes <- "Could not find coverPageHeader element" :: notes
                
                // Parse coverPageHeaderReportingPersonDetails (filer name, ownership)
                let reportingPerson = 
                    match ns with
                    | Some nsUri -> form.Descendants(xnameWithNs nsUri "coverPageHeaderReportingPersonDetails") |> Seq.tryHead
                    | None -> form.Descendants(xname "coverPageHeaderReportingPersonDetails") |> Seq.tryHead
                
                match reportingPerson with
                | Some person ->
                    // Filer/reporting person name
                    let filerName = 
                        match ns with
                        | Some nsUri -> tryGetElementValueNs person nsUri "reportingPersonName"
                        | None -> tryGetElementValue person "reportingPersonName"
                    
                    parsed <- { parsed with FilerName = defaultArg filerName "" }
                    
                    // Aggregate shares owned
                    let totalShares = 
                        match ns with
                        | Some nsUri -> tryGetElementValueNs person nsUri "reportingPersonBeneficiallyOwnedAggregateNumberOfShares"
                        | None -> tryGetElementValue person "reportingPersonBeneficiallyOwnedAggregateNumberOfShares"
                    
                    parsed <- { parsed with SharesOwned = defaultArg (tryParseInt64 totalShares) 0L }
                    
                    // Percentage of class
                    let percentStr = 
                        match ns with
                        | Some nsUri -> tryGetElementValueNs person nsUri "classPercent"
                        | None -> tryGetElementValue person "classPercent"
                    
                    parsed <- { parsed with PercentOfClass = defaultArg (tryParseDecimal percentStr) 0.0m }
                    
                    // Entity type
                    let entityType = 
                        match ns with
                        | Some nsUri -> tryGetElementValueNs person nsUri "typeOfReportingPerson"
                        | None -> tryGetElementValue person "typeOfReportingPerson"
                    
                    parsed <- { parsed with EntityType = Schedule13GHelpers.mapEntityType entityType }
                    
                    // Voting and dispositive powers
                    let powerInfo = 
                        match ns with
                        | Some nsUri -> person.Descendants(xnameWithNs nsUri "reportingPersonBeneficiallyOwnedNumberOfShares") |> Seq.tryHead
                        | None -> person.Descendants(xname "reportingPersonBeneficiallyOwnedNumberOfShares") |> Seq.tryHead
                    
                    match powerInfo with
                    | Some powers ->
                        let soleVoting = 
                            match ns with
                            | Some nsUri -> tryGetElementValueNs powers nsUri "soleVotingPower"
                            | None -> tryGetElementValue powers "soleVotingPower"
                        
                        let sharedVoting = 
                            match ns with
                            | Some nsUri -> tryGetElementValueNs powers nsUri "sharedVotingPower"
                            | None -> tryGetElementValue powers "sharedVotingPower"
                        
                        let soleDispositive = 
                            match ns with
                            | Some nsUri -> tryGetElementValueNs powers nsUri "soleDispositivePower"
                            | None -> tryGetElementValue powers "soleDispositivePower"
                        
                        let sharedDispositive = 
                            match ns with
                            | Some nsUri -> tryGetElementValueNs powers nsUri "sharedDispositivePower"
                            | None -> tryGetElementValue powers "sharedDispositivePower"
                        
                        parsed <- { parsed with 
                                      SoleVotingPower = tryParseInt64 soleVoting
                                      SharedVotingPower = tryParseInt64 sharedVoting
                                      SoleDispositivePower = tryParseInt64 soleDispositive
                                      SharedDispositivePower = tryParseInt64 sharedDispositive }
                    | None ->
                        notes <- "Could not find reportingPersonBeneficiallyOwnedNumberOfShares element" :: notes
                    
                    logger |> Option.iter (fun l -> 
                        l.LogDebug("Found reporting person: {name}, {shares} shares ({percent}%)", 
                                   parsed.FilerName, parsed.SharesOwned, parsed.PercentOfClass))
                | None ->
                    notes <- "Could not find coverPageHeaderReportingPersonDetails element" :: notes
                
                // Parse signatureInformation for filing date
                let sigInfo = 
                    match ns with
                    | Some nsUri -> form.Descendants(xnameWithNs nsUri "signatureInformation") |> Seq.tryHead
                    | None -> form.Descendants(xname "signatureInformation") |> Seq.tryHead
                
                match sigInfo with
                | Some signature ->
                    let sigDetails = 
                        match ns with
                        | Some nsUri -> signature.Descendants(xnameWithNs nsUri "signatureDetails") |> Seq.tryHead
                        | None -> signature.Descendants(xname "signatureDetails") |> Seq.tryHead
                    
                    match sigDetails with
                    | Some details ->
                        let dateStr = 
                            match ns with
                            | Some nsUri -> tryGetElementValueNs details nsUri "date"
                            | None -> tryGetElementValue details "date"
                        
                        parsed <- { parsed with FilingDate = defaultArg (tryParseDate dateStr) DateTimeOffset.UtcNow }
                    | None ->
                        notes <- "Could not find signatureDetails element" :: notes
                | None ->
                    notes <- "Could not find signatureInformation element" :: notes
            | None ->
                notes <- "Could not find formData element" :: notes
            
            // Calculate confidence and attach notes
            let confidence = Schedule13GHelpers.calculateConfidence parsed
            let parsingNotes = List.rev notes
            let parsedWithMeta = 
                { parsed with 
                    Confidence = confidence
                    ParsingNotes = parsingNotes }
            
            logger |> Option.iter (fun l -> 
                l.LogInformation("Parsed Schedule 13G with confidence {confidence:F2}", confidence))
            
            // Determine result type based on confidence
            if confidence >= 0.7 && notes.Length = 0 then
                Success parsedWithMeta
            elif confidence >= 0.5 then
                PartialSuccess (parsedWithMeta, notes)
            else
                let notesStr = String.Join("; ", notes)
                Failure $"Low confidence parse ({confidence:F2}). Notes: {notesStr}"
            
        with ex ->
            logger |> Option.iter (fun l -> 
                l.LogError(ex, "Error parsing Schedule 13G XML"))
            Failure $"XML parsing error: {ex.Message}"
    
    /// Parse from XML file path
    let parseFromFile (filePath: string) (logger: ILogger option) =
        try
            let xml = File.ReadAllText(filePath)
            parseXml xml logger
        with ex ->
            logger |> Option.iter (fun l -> 
                l.LogError(ex, "Error reading Schedule 13G file: {path}", filePath))
            Failure $"File read error: {ex.Message}"
    
    /// Parse from URL (using HttpClient with proper SEC headers)
    let parseFromUrl (url: string) (httpClient: HttpClient) (logger: ILogger option) =
        async {
            try
                logger |> Option.iter (fun l -> 
                    l.LogDebug("Fetching Schedule 13G from URL: {url}", url))
                
                let! response = httpClient.GetAsync(url) |> Async.AwaitTask
                response.EnsureSuccessStatusCode() |> ignore
                
                let! xml = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                return parseXml xml logger
                
            with ex ->
                logger |> Option.iter (fun l -> 
                    l.LogError(ex, "Error fetching Schedule 13G from URL: {url}", url))
                return Failure $"HTTP fetch error: {ex.Message}"
        }

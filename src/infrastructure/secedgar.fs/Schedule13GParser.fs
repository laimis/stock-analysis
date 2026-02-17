namespace secedgar.fs

open System
open System.IO
open System.Net.Http
open System.Xml
open System.Xml.Linq
open Microsoft.Extensions.Logging

// used this for testing https://www.sec.gov/Archives/edgar/data/315066/000031506626000439/primary_doc.xml
// human friendly: https://www.sec.gov/Archives/edgar/data/1516513/000031506626000439/xslSCHEDULE_13G_X01/primary_doc.xml
// TODO: Implement a mechanism to retrieve XML versions for all relevant SEC Schedule 13G documents.

module Schedule13GParser =
    
    let private xname name = XName.Get(name)
    let private xnameWithNs ns name = XName.Get(name, ns)
    
    /// Create secure XmlReader to prevent XXE attacks
    let private createSecureXmlReader (xml: string) =
        let settings = XmlReaderSettings()
        settings.DtdProcessing <- DtdProcessing.Prohibit
        settings.XmlResolver <- null
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
            Some el.Value
        else
            None
    
    /// Try to get first descendant element with optional namespace
    let private tryGetDescendant (element: XElement) (ns: string option) (name: string) =
        element.Descendants(getXName ns name) |> Seq.tryHead
    
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
            use reader = createSecureXmlReader xml
            let doc = XDocument.Load(reader)
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
            let headerData = tryGetDescendant root ns "headerData"
            
            match headerData with
            | Some header ->
                // Get submission type (SCHEDULE 13G or SCHEDULE 13G/A)
                let submissionType = tryGetElementValue header ns "submissionType"
                
                parsed <- { parsed with IsAmendment = submissionType |> Option.exists (fun s -> s.Contains("/A")) }
                
                // Get filer CIK from headerData
                let filerInfo = tryGetDescendant header ns "filerInfo"
                
                match filerInfo with
                | Some info ->
                    let creds = tryGetDescendant info ns "filerCredentials"
                    
                    match creds with
                    | Some c ->
                        let cik = tryGetElementValue c ns "cik"
                        
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
            let formData = tryGetDescendant root ns "formData"
            
            match formData with
            | Some form ->
                // Parse coverPageHeader (issuer info, dates)
                let coverPageHeader = tryGetDescendant form ns "coverPageHeader"
                
                match coverPageHeader with
                | Some header ->
                    // Get issuer information
                    let issuerInfo = tryGetDescendant header ns "issuerInfo"
                    
                    match issuerInfo with
                    | Some issuer ->
                        let issuerName = tryGetElementValue issuer ns "issuerName"
                        let issuerCik = tryGetElementValue issuer ns "issuerCik"
                        
                        parsed <- { parsed with 
                                      IssuerName = defaultArg issuerName ""
                                      IssuerCik = issuerCik }
                        
                        logger |> Option.iter (fun l -> 
                            l.LogDebug("Found issuer: {name} (CIK: {cik})", parsed.IssuerName, issuerCik))
                    | None ->
                        notes <- "Could not find issuerInfo element" :: notes
                    
                    // Get event date (as-of date)
                    let eventDate = tryGetElementValue header ns "eventDateRequiresFilingThisStatement"
                    
                    parsed <- { parsed with AsOfDate = tryParseDate eventDate }
                | None ->
                    notes <- "Could not find coverPageHeader element" :: notes
                
                // Parse coverPageHeaderReportingPersonDetails (filer name, ownership)
                let reportingPerson = tryGetDescendant form ns "coverPageHeaderReportingPersonDetails"
                
                match reportingPerson with
                | Some person ->
                    // Filer/reporting person name
                    let filerName = tryGetElementValue person ns "reportingPersonName"
                    
                    parsed <- { parsed with FilerName = defaultArg filerName "" }
                    
                    // Aggregate shares owned
                    let totalShares = tryGetElementValue person ns "reportingPersonBeneficiallyOwnedAggregateNumberOfShares"
                    
                    parsed <- { parsed with SharesOwned = defaultArg (tryParseInt64 totalShares) 0L }
                    
                    // Percentage of class
                    let percentStr = tryGetElementValue person ns "classPercent"
                    
                    parsed <- { parsed with PercentOfClass = defaultArg (tryParseDecimal percentStr) 0.0m }
                    
                    // Entity type
                    let entityType = tryGetElementValue person ns "typeOfReportingPerson"
                    
                    parsed <- { parsed with EntityType = Schedule13GHelpers.mapEntityType entityType }
                    
                    // Voting and dispositive powers
                    let powerInfo = tryGetDescendant person ns "reportingPersonBeneficiallyOwnedNumberOfShares"
                    
                    match powerInfo with
                    | Some powers ->
                        let soleVoting = tryGetElementValue powers ns "soleVotingPower"
                        let sharedVoting = tryGetElementValue powers ns "sharedVotingPower"
                        let soleDispositive = tryGetElementValue powers ns "soleDispositivePower"
                        let sharedDispositive = tryGetElementValue powers ns "sharedDispositivePower"
                        
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
                let sigInfo = tryGetDescendant form ns "signatureInformation"
                
                match sigInfo with
                | Some signature ->
                    let sigDetails = tryGetDescendant signature ns "signatureDetails"
                    
                    match sigDetails with
                    | Some details ->
                        let dateStr = tryGetElementValue details ns "date"
                        
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
    let parseFromFile (logger: ILogger option) (filePath: string) =
        try
            let xml = File.ReadAllText(filePath)
            parseXml xml logger
        with ex ->
            logger |> Option.iter (fun l -> 
                l.LogError(ex, "Error reading Schedule 13G file: {path}", filePath))
            Failure $"File read error: {ex.Message}"
    
    /// Parse from URL (using HttpClient with proper SEC headers)
    let parseFromDocument (logger: ILogger option) (xml: string) =
        try
            logger |> Option.iter (fun l -> 
                l.LogDebug "Parsing XML document")
            
            parseXml xml logger
        with ex ->
            logger |> Option.iter (fun l -> 
                l.LogError(ex, "Error parsing Schedule 13G XML document"))
            Failure $"XML parsing error: {ex.Message}"

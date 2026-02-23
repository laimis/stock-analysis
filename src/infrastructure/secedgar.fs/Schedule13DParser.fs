namespace secedgar.fs

open System
open System.IO
open System.Xml
open System.Xml.Linq
open Microsoft.Extensions.Logging

// Used for testing: https://www.sec.gov/Archives/edgar/data/1361570/000136157026000003/primary_doc.xml
// Full filing index: https://www.sec.gov/Archives/edgar/data/1707753/000136157026000003/0001361570-26-000003-index.html
// Issuer: Elastic N.V. (ESTC), Filer: Pictet Asset Management SA

module Schedule13DParser =
    open EdgarParserHelpers
    
    /// Parse Schedule 13D from XML document.
    /// Schedule 13D filings contain a structured <reportingPersons> section with all ownership
    /// data in discrete XML fields - no regex extraction is needed.
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
                    let secClass = tryGetElementValue header ns "securitiesClassTitle"
                    parsed <- { parsed with SecuritiesClassTitle = secClass }
                    
                    // Date of event (MM/dd/yyyy format in 13D unlike 13G's yyyy-MM-dd)
                    let dateOfEvent = tryGetElementValue header ns "dateOfEvent"
                    parsed <- { parsed with AsOfDate = tryParseDate dateOfEvent }
                    
                    if dateOfEvent.IsSome && parsed.AsOfDate.IsNone then
                        notes <- $"Could not parse dateOfEvent: '{dateOfEvent.Value}'" :: notes
                    
                    let issuerInfo = tryGetDescendant header ns "issuerInfo"
                    match issuerInfo with
                    | Some issuer ->
                        let issuerName  = tryGetElementValue issuer ns "issuerName"
                        let issuerCik   = tryGetElementValue issuer ns "issuerCIK"
                        let issuerCusip = tryGetElementValue issuer ns "issuerCUSIP"
                        parsed <- { parsed with 
                                      IssuerName  = defaultArg issuerName ""
                                      IssuerCik   = issuerCik
                                      IssuerCusip = issuerCusip }
                        logger |> Option.iter (fun l -> 
                            l.LogDebug("Found issuer: {name} (CIK: {cik})", parsed.IssuerName, issuerCik))
                    | None ->
                        notes <- "Could not find issuerInfo element" :: notes
                | None ->
                    notes <- "Could not find coverPageHeader element" :: notes
                
                // --- item2: full citizenship text (e.g. "SWITZERLAND") ---
                let item2 = tryGetDescendant form ns "item2"
                match item2 with
                | Some i2 ->
                    let citizenship = tryGetElementValue i2 ns "citizenship"
                    parsed <- { parsed with Citizenship = citizenship }
                | None -> ()  // optional
                
                // --- item4: purpose of acquisition ---
                let item4 = tryGetDescendant form ns "item4"
                match item4 with
                | Some i4 ->
                    let purpose = tryGetElementValue i4 ns "transactionPurpose"
                    if purpose.IsSome then
                        let trimmed = 
                            purpose.Value
                            |> fun s -> if s.Length > 1000 then s.[..999] + "..." else s
                        parsed <- { parsed with AcquisitionPurpose = Some trimmed }
                | None -> ()  // optional
                
                // --- reportingPersons: structured ownership data (no regex needed) ---
                // The SEC 13D XML provides all share counts and voting powers as discrete elements.
                let reportingInfo = tryGetDescendant form ns "reportingPersonInfo"
                match reportingInfo with
                | Some info ->
                    let personName  = tryGetElementValue info ns "reportingPersonName"
                    let personCIK   = tryGetElementValue info ns "reportingPersonCIK"
                    let entityCode  = tryGetElementValue info ns "typeOfReportingPerson"
                    let sharesOwned = tryParseInt64 (tryGetElementValue info ns "aggregateAmountOwned")
                    let pctOfClass  = tryParseDecimal (tryGetElementValue info ns "percentOfClass")
                    let soleVoting  = tryParseInt64 (tryGetElementValue info ns "soleVotingPower")
                    let sharedVoting = tryParseInt64 (tryGetElementValue info ns "sharedVotingPower")
                    let soleDisp    = tryParseInt64 (tryGetElementValue info ns "soleDispositivePower")
                    let sharedDisp  = tryParseInt64 (tryGetElementValue info ns "sharedDispositivePower")
                    
                    // Use reportingPersonInfo name/CIK if available (may override filerCredentials CIK)
                    if personName.IsSome then
                        parsed <- { parsed with FilerName = personName.Value }
                    if personCIK.IsSome then
                        parsed <- { parsed with FilerCik = personCIK }
                    
                    parsed <- { parsed with
                                  EntityType             = Schedule13DHelpers.mapEntityType entityCode
                                  SharesOwned            = defaultArg sharesOwned 0L
                                  PercentOfClass         = defaultArg pctOfClass 0.0m
                                  SoleVotingPower        = soleVoting
                                  SharedVotingPower      = sharedVoting
                                  SoleDispositivePower   = soleDisp
                                  SharedDispositivePower = sharedDisp }
                    
                    if sharesOwned.IsNone then
                        notes <- "Could not parse aggregateAmountOwned from reportingPersonInfo" :: notes
                    if pctOfClass.IsNone then
                        notes <- "Could not parse percentOfClass from reportingPersonInfo" :: notes
                    
                    logger |> Option.iter (fun l ->
                        l.LogDebug("Parsed reportingPersonInfo: {shares} shares, {pct}%, type={type}",
                                   parsed.SharesOwned, parsed.PercentOfClass, defaultArg entityCode "?"))
                | None ->
                    notes <- "Could not find reportingPersonInfo element" :: notes
                
                // --- signatureInfo: filer name (fallback) & filing date ---
                let sigInfo = tryGetDescendant form ns "signatureInfo"
                match sigInfo with
                | Some sigI ->
                    let sigPerson = tryGetDescendant sigI ns "signaturePerson"
                    match sigPerson with
                    | Some sp ->
                        // Only use signature name as fallback if reportingPersonInfo didn't supply one
                        if String.IsNullOrWhiteSpace parsed.FilerName then
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
            
            // Calculate confidence and attach notes
            let confidence = Schedule13DHelpers.calculateConfidence parsed
            let parsingNotes = List.rev notes
            let parsedWithMeta = 
                { parsed with 
                    Confidence   = confidence
                    ParsingNotes = parsingNotes }
            
            logger |> Option.iter (fun l -> 
                l.LogInformation("Parsed Schedule 13D with confidence {confidence:F2}", confidence))
            
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

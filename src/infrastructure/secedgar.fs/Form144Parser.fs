namespace secedgar.fs

open System
open System.IO
open System.Xml
open System.Xml.Linq
open Microsoft.Extensions.Logging

// Form 144 - Report of Proposed Sale of Securities
// Real example: https://www.sec.gov/Archives/edgar/data/1321655/000195004726001584/0001950047-26-001584-index.html
// XML example:  https://www.sec.gov/Archives/edgar/data/1321655/000195004726001584/primary_doc.xml

module Form144Parser =
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
            Some el.Value
        else
            None

    /// Try to get first descendant element with optional namespace
    let private tryGetDescendant (element: XElement) (ns: string option) (name: string) =
        element.Descendants(getXName ns name) |> Seq.tryHead

    /// Try to get all descendant elements with optional namespace
    let private getAllDescendants (element: XElement) (ns: string option) (name: string) =
        element.Descendants(getXName ns name) |> Seq.toList

    /// Try to parse int64 from string, handling commas and whitespace
    let private tryParseInt64 (value: string option) =
        match value with
        | None -> None
        | Some v ->
            let cleaned = v.Trim().Replace(",", "").Replace(" ", "")
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

    /// Try to parse DateTimeOffset from ISO format (yyyy-MM-dd)
    let private tryParseIsoDate (value: string option) =
        match value with
        | None -> None
        | Some v ->
            match DateTimeOffset.TryParseExact(v.Trim(), "yyyy-MM-dd", null, DateTimeStyles.None) with
            | (true, date) -> Some date
            | _ -> None

    /// Try to parse DateTimeOffset from MM/dd/yyyy format (used in Form 144)
    let private tryParseUsDate (value: string option) =
        match value with
        | None -> None
        | Some v ->
            let trimmed = v.Trim()
            // Try MM/dd/yyyy first, then yyyy-MM-dd as fallback
            match DateTimeOffset.TryParseExact(trimmed, "MM/dd/yyyy", null, DateTimeStyles.None) with
            | (true, date) -> Some date
            | _ ->
                match DateTimeOffset.TryParseExact(trimmed, "yyyy-MM-dd", null, DateTimeStyles.None) with
                | (true, date) -> Some date
                | _ -> None

    /// Parse Form 144 from XML document
    let parseXml (xml: string) (logger: ILogger option) =
        try
            use reader = createSecureXmlReader xml
            let doc = XDocument.Load(reader)
            let root = doc.Root

            let mutable notes = []
            let mutable parsed = Form144Helpers.empty

            // Set raw XML for debugging
            parsed <- { parsed with RawXml = Some xml }

            // Detect namespace
            let ns =
                if root.Name.NamespaceName <> "" then
                    Some root.Name.NamespaceName
                else
                    None

            logger |> Option.iter (fun l ->
                l.LogDebug("Parsing Form 144 XML, namespace: {namespace}", defaultArg ns "none"))

            // Parse headerData section - get filer CIK
            let headerData = tryGetDescendant root ns "headerData"

            match headerData with
            | Some header ->
                // Get submission type (144 or 144/A)
                let submissionType = tryGetElementValue header ns "submissionType"
                parsed <- { parsed with IsAmendment = submissionType |> Option.exists (fun s -> s.Contains("/A")) }

                // Get filer CIK from headerData
                let filerCredentials = tryGetDescendant header ns "filerCredentials"
                match filerCredentials with
                | Some creds ->
                    let cik = tryGetElementValue creds ns "cik"
                    parsed <- { parsed with FilerCik = cik }
                    logger |> Option.iter (fun l ->
                        l.LogDebug("Found filer CIK: {cik}", cik))
                | None ->
                    notes <- "Could not find filerCredentials element" :: notes
            | None ->
                notes <- "Could not find headerData element" :: notes

            // Parse formData section - main content
            let formData = tryGetDescendant root ns "formData"

            match formData with
            | Some form ->

                // === issuerInfo section ===
                let issuerInfo = tryGetDescendant form ns "issuerInfo"

                match issuerInfo with
                | Some issuer ->
                    let issuerName = tryGetElementValue issuer ns "issuerName"
                    let issuerCik = tryGetElementValue issuer ns "issuerCik"
                    let personName = tryGetElementValue issuer ns "nameOfPersonForWhoseAccountTheSecuritiesAreToBeSold"

                    parsed <- { parsed with
                                    IssuerName = defaultArg issuerName ""
                                    IssuerCik = issuerCik
                                    PersonName = (personName |> Option.map (fun n -> n.Trim()) |> Option.defaultValue "") }

                    // Parse all relationship to issuer values
                    let relationships = getAllDescendants issuer ns "relationshipToIssuer"
                    let relationshipValues =
                        relationships
                        |> List.map (fun el -> el.Value.Trim())
                        |> List.filter (fun v -> not (String.IsNullOrWhiteSpace v))

                    parsed <- { parsed with RelationshipsToIssuer = relationshipValues }

                    logger |> Option.iter (fun l ->
                        l.LogDebug("Found issuer: {name} (CIK: {cik}), person: {person}, relationships: {rels}",
                                   parsed.IssuerName, issuerCik, parsed.PersonName,
                                   String.Join(", ", relationshipValues)))
                | None ->
                    notes <- "Could not find issuerInfo element" :: notes

                // === securitiesInformation section ===
                let secInfo = tryGetDescendant form ns "securitiesInformation"

                match secInfo with
                | Some sec ->
                    let classTitle = tryGetElementValue sec ns "securitiesClassTitle"
                    let unitsToSell = tryGetElementValue sec ns "noOfUnitsSold"
                    let marketValue = tryGetElementValue sec ns "aggregateMarketValue"
                    let unitsOutstanding = tryGetElementValue sec ns "noOfUnitsOutstanding"
                    let saleDate = tryGetElementValue sec ns "approxSaleDate"
                    let exchange = tryGetElementValue sec ns "securitiesExchangeName"

                    parsed <- { parsed with
                                    SecuritiesClassTitle = classTitle
                                    SharesToSell = defaultArg (tryParseInt64 unitsToSell) 0L
                                    AggregateMarketValue = tryParseDecimal marketValue
                                    SharesOutstanding = tryParseInt64 unitsOutstanding
                                    ApproxSaleDate = tryParseUsDate saleDate
                                    Exchange = exchange }

                    if parsed.SharesToSell = 0L then
                        notes <- "noOfUnitsSold is 0 or could not be parsed" :: notes

                    logger |> Option.iter (fun l ->
                        l.LogDebug("Found securities info: {shares} shares, market value {value}, sale date {date}",
                                   parsed.SharesToSell, parsed.AggregateMarketValue, parsed.ApproxSaleDate))
                | None ->
                    notes <- "Could not find securitiesInformation element" :: notes

                // === securitiesToBeSold section ===
                let securitiesToBeSold = tryGetDescendant form ns "securitiesToBeSold"

                match securitiesToBeSold with
                | Some toBeSold ->
                    let natureOfAcquisition = tryGetElementValue toBeSold ns "natureOfAcquisitionTransaction"
                    let acquisitionAmount = tryGetElementValue toBeSold ns "amountOfSecuritiesAcquired"

                    parsed <- { parsed with
                                    NatureOfAcquisition = natureOfAcquisition
                                    SecuritiesAcquired = tryParseInt64 acquisitionAmount }
                | None ->
                    // Not critical - some Form 144 filings may not have this section
                    logger |> Option.iter (fun l ->
                        l.LogDebug("securitiesToBeSold section not found (may be normal for some Form 144 filings)"))

                // === noticeSignature section for dates ===
                let noticeSignature = tryGetDescendant form ns "noticeSignature"

                match noticeSignature with
                | Some signature ->
                    let noticeDate = tryGetElementValue signature ns "noticeDate"
                    parsed <- { parsed with NoticeDate = tryParseUsDate noticeDate }

                    // Plan adoption date (Rule 10b5-1)
                    let planAdoptionDate = tryGetDescendant signature ns "planAdoptionDate"
                    match planAdoptionDate with
                    | Some planEl ->
                        if not (String.IsNullOrWhiteSpace planEl.Value) then
                            parsed <- { parsed with PlanAdoptionDate = tryParseUsDate (Some planEl.Value) }
                    | None -> ()
                | None ->
                    notes <- "Could not find noticeSignature element" :: notes

                // === nothingToReportFlag ===
                let nothingToReport = tryGetElementValue form ns "nothingToReportFlagOnSecuritiesSoldInPast3Months"
                parsed <- { parsed with
                                NothingToReportPast3Months = nothingToReport |> Option.exists (fun v -> v.Trim().ToUpper() = "Y") }

            | None ->
                notes <- "Could not find formData element" :: notes

            // Calculate confidence and finalize
            let confidence = Form144Helpers.calculateConfidence parsed
            let parsingNotes = List.rev notes
            let parsedWithMeta =
                { parsed with
                    Confidence = confidence
                    ParsingNotes = parsingNotes }

            logger |> Option.iter (fun l ->
                l.LogInformation("Parsed Form 144 with confidence {confidence:F2}, person: {person}, shares: {shares}",
                                 confidence, parsedWithMeta.PersonName, parsedWithMeta.SharesToSell))

            // Determine result based on confidence
            if confidence >= 0.7 && notes.Length = 0 then
                Form144Success parsedWithMeta
            elif confidence >= 0.5 then
                Form144PartialSuccess (parsedWithMeta, notes)
            else
                let notesStr = String.Join("; ", notes)
                Form144Failure $"Low confidence parse ({confidence:F2}). Notes: {notesStr}"

        with ex ->
            logger |> Option.iter (fun l ->
                l.LogError(ex, "Error parsing Form 144 XML"))
            Form144Failure $"XML parsing error: {ex.Message}"

    /// Parse from XML file path
    let parseFromFile (logger: ILogger option) (filePath: string) =
        try
            let xml = File.ReadAllText(filePath)
            parseXml xml logger
        with ex ->
            logger |> Option.iter (fun l ->
                l.LogError(ex, "Error reading Form 144 file: {path}", filePath))
            Form144Failure $"File read error: {ex.Message}"

    /// Parse from XML document string
    let parseFromDocument (logger: ILogger option) (xml: string) =
        try
            logger |> Option.iter (fun l ->
                l.LogDebug("Parsing Form 144 XML document"))
            parseXml xml logger
        with ex ->
            logger |> Option.iter (fun l ->
                l.LogError(ex, "Error parsing Form 144 XML document"))
            Form144Failure $"XML parsing error: {ex.Message}"

namespace secedgar.fs

open System
open Microsoft.Extensions.Logging
open core.fs
open core.fs.Adapters.SEC
open core.fs.Adapters.Storage
open core.Shared

/// Service that processes Form 144 (Proposed Sale of Securities) filings from the SEC
/// and stores them as ownership events in the database.
type Form144ProcessingService(
    secFilingStorage: ISECFilingStorage,
    ownershipStorage: IOwnershipStorage,
    secFilings: ISECFilings,
    logger: ILogger<Form144ProcessingService>) =

    let isForm144 (formType: string) =
        formType.Equals("144", StringComparison.OrdinalIgnoreCase)
        || formType.Equals("144/A", StringComparison.OrdinalIgnoreCase)

    let findOrCreateEntity (parsed: ParsedForm144) = task {
        // Try to find entity by CIK first (most reliable)
        match parsed.FilerCik with
        | Some cik ->
            let! existingEntity = ownershipStorage.FindEntityByCik cik
            match existingEntity with
            | Some entity ->
                logger.LogInformation($"Found existing entity by CIK: {entity.Name} ({cik})")
                return entity
            | None ->
                // Create new entity - Form 144 filers are typically insiders (EP = Executive/C-Suite)
                let entityType = Form144Helpers.determineEntityType parsed.RelationshipsToIssuer |> Option.defaultValue "EP"
                let newEntity = OwnershipEntity.create parsed.PersonName entityType parsed.FilerCik
                logger.LogInformation($"Creating new entity: {parsed.PersonName} (CIK: {cik}, type: {entityType})")
                let! entityId = ownershipStorage.SaveEntity newEntity
                return { newEntity with Id = entityId }
        | None ->
            // No CIK - try to find by name
            let! existingEntities = ownershipStorage.FindEntitiesByName parsed.PersonName
            let existingEntity = existingEntities |> Seq.tryHead

            match existingEntity with
            | Some entity ->
                logger.LogInformation($"Found existing entity by name: {entity.Name}")
                return entity
            | None ->
                // Create new entity without CIK
                let entityType = Form144Helpers.determineEntityType parsed.RelationshipsToIssuer |> Option.defaultValue "EP"
                let newEntity = OwnershipEntity.create parsed.PersonName entityType None
                logger.LogInformation($"Creating new entity without CIK: {parsed.PersonName}")
                let! entityId = ownershipStorage.SaveEntity newEntity
                return { newEntity with Id = entityId }
    }

    let createOwnershipEvent (entity: OwnershipEntity) (parsed: ParsedForm144) (filing: SECFilingRecord) (ticker: Ticker) =
        // Form 144 is always an "intent_to_sell" event
        let eventType = "intent_to_sell"

        // Transaction type is always a sale
        let transactionType = Some "sale"

        // Use approximate sale date if available, otherwise use the notice date, or filing date
        let transactionDate =
            match parsed.ApproxSaleDate with
            | Some d -> d.ToString("yyyy-MM-dd")
            | None ->
                match parsed.NoticeDate with
                | Some d -> d.ToString("yyyy-MM-dd")
                | None -> filing.FilingDate

        // Calculate price per share from aggregate market value and shares to sell
        let pricePerShare =
            match parsed.AggregateMarketValue with
            | Some value when parsed.SharesToSell > 0L ->
                Some (value / decimal parsed.SharesToSell)
            | _ -> None

        // Describe the nature of this Form 144 event with available context
        let ownershipNature =
            match parsed.NatureOfAcquisition with
            | Some nature -> Some $"Proposed sale of {nature}"
            | None -> Some "Proposed sale (Form 144)"

        OwnershipEvent.create
            entity.Id
            ticker.Value
            filing.Cik
            (Some filing.Id)
            eventType
            transactionType
            None                                   // No shares before (not provided in Form 144)
            (Some parsed.SharesToSell)             // Shares being proposed for sale
            None
            None                                   // Percent of class not directly in Form 144
            pricePerShare
            parsed.AggregateMarketValue
            transactionDate
            filing.FilingDate
            true                                   // Form 144 is always direct ownership
            ownershipNature

    let processForm144Filing (filing: SECFilingRecord) = task {
        try
            logger.LogInformation($"Processing Form 144 filing for {filing.Ticker}: {filing.FilingUrl}")

            // Check if this filing has already been processed
            let ticker = Ticker filing.Ticker
            let! existingEvents = ownershipStorage.GetEventsByCompany ticker
            let alreadyProcessed =
                existingEvents
                |> Seq.exists (fun e -> e.FilingId = Some filing.Id)

            if alreadyProcessed then
                logger.LogInformation($"Filing already processed, skipping: {filing.FilingUrl}")
                return Ok ()
            else
                // Fetch the primary XML document
                let! xmlResult = secFilings.FetchPrimaryDocument filing.FilingUrl

                match xmlResult with
                | Error err ->
                    logger.LogError($"Failed to fetch XML for {filing.Ticker}: {err}")
                    return Error $"Failed to fetch XML: {err}"
                | Ok xmlContent ->
                    // Parse the XML using Form144Parser
                    let parseResult = Form144Parser.parseFromDocument (Some logger) xmlContent

                    match parseResult with
                    | Form144Success parsed ->
                        logger.LogInformation(
                            $"Successfully parsed Form 144 for {filing.Ticker}: {parsed.PersonName}, " +
                            $"{parsed.SharesToSell} shares, market value {parsed.AggregateMarketValue}")

                        // Find or create entity
                        let! entity = findOrCreateEntity parsed

                        // Create ownership event
                        let ownershipEvent = createOwnershipEvent entity parsed filing ticker

                        // Save the event
                        let! eventId = ownershipStorage.SaveEvent ownershipEvent
                        logger.LogInformation($"Created ownership event {eventId} for entity {entity.Name} and ticker {filing.Ticker}")

                        return Ok ()

                    | Form144PartialSuccess (parsed, notes) ->
                        let notesStr = String.Join("; ", notes |> List.toArray)
                        logger.LogWarning($"Partial success parsing Form 144 for {filing.Ticker}. Confidence: {parsed.Confidence:F2}. Notes: {notesStr}")

                        // Still create the event if confidence is reasonable
                        if parsed.Confidence >= 0.5 then
                            let! entity = findOrCreateEntity parsed
                            let ownershipEvent = createOwnershipEvent entity parsed filing ticker
                            let! eventId = ownershipStorage.SaveEvent ownershipEvent
                            logger.LogInformation($"Created ownership event {eventId} despite partial success (confidence {parsed.Confidence:F2})")
                            return Ok ()
                        else
                            logger.LogError($"Confidence too low ({parsed.Confidence:F2}) to create ownership event")
                            return Error $"Low confidence parse: {notesStr}"

                    | Form144Failure msg ->
                        logger.LogError($"Failed to parse Form 144 for {filing.Ticker}: {msg}")
                        return Error $"Parse failure: {msg}"

        with ex ->
            logger.LogError(ex, "Error processing Form 144 filing for {Ticker}: {Message}", filing.Ticker, ex.Message)
            return Error ex.Message
    }

    interface IApplicationService

    /// Exposed for inline invocation from SECFilingsSyncService after saving new filings.
    member _.ProcessFiling(filing: SECFilingRecord) = processForm144Filing filing

    member _.Execute() = task {
        try
            logger.LogInformation("Starting Form 144 catch-up processing service")

            // Query for Form 144 filings that have no ownership events yet (missed or failed during sync).
            let formTypes = [| "144"; "144/A" |]
            let! unprocessedFilings = secFilingStorage.GetFilingsWithoutOwnershipEvents formTypes

            let filings =
                unprocessedFilings
                |> Seq.filter (fun f -> isForm144 f.FormType)
                |> Seq.toArray

            logger.LogInformation($"Found {filings.Length} unprocessed Form 144/144-A filings")

            if filings.Length = 0 then
                logger.LogInformation("No Form 144 filings to process")
            else
                let mutable successCount = 0
                let mutable failureCount = 0

                for filing in filings do
                    let! result = processForm144Filing filing
                    match result with
                    | Ok _ -> successCount <- successCount + 1
                    | Error _ -> failureCount <- failureCount + 1

                    // Rate limiting: respect SEC's 10 requests/second limit
                    do! System.Threading.Tasks.Task.Delay(500)

                logger.LogInformation($"Form 144 processing completed. Success: {successCount}, Failures: {failureCount}")

        with ex ->
            logger.LogError(ex, "Error in Form 144 catch-up processing service: {Message}", ex.Message)
    }

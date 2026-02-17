namespace secedgar.fs

open System
open System.Linq
open Microsoft.Extensions.Logging
open core.fs
open core.fs.Adapters.SEC
open core.fs.Adapters.Storage
open core.fs.Services
open core.Shared

type Schedule13GProcessingService(
    secFilingStorage: ISECFilingStorage,
    ownershipStorage: IOwnershipStorage,
    secFilings: ISECFilings,
    logger: ILogger<Schedule13GProcessingService>) =

    let isSchedule13G (formType: string) =
        formType.Contains("13G", StringComparison.OrdinalIgnoreCase)

    let findOrCreateEntity (parsed: ParsedSchedule13G) = task {
        // Try to find entity by CIK first (most reliable)
        match parsed.FilerCik with
        | Some cik ->
            let! existingEntity = ownershipStorage.FindEntityByCik cik
            match existingEntity with
            | Some entity ->
                logger.LogInformation($"Found existing entity by CIK: {entity.Name} ({cik})")
                return entity
            | None ->
                // Create new entity
                let entityType = defaultArg parsed.EntityType "OO"
                let newEntity = OwnershipEntity.create parsed.FilerName entityType parsed.FilerCik
                logger.LogInformation($"Creating new entity: {parsed.FilerName} (CIK: {cik})")
                let! entityId = ownershipStorage.SaveEntity newEntity
                return { newEntity with Id = entityId }
        | None ->
            // No CIK - try to find by name
            let! existingEntities = ownershipStorage.FindEntitiesByName parsed.FilerName
            let existingEntity = existingEntities |> Seq.tryHead
            
            match existingEntity with
            | Some entity ->
                logger.LogInformation($"Found existing entity by name: {entity.Name}")
                return entity
            | None ->
                // Create new entity without CIK
                let entityType = defaultArg parsed.EntityType "OO"
                let newEntity = OwnershipEntity.create parsed.FilerName entityType None
                logger.LogInformation($"Creating new entity without CIK: {parsed.FilerName}")
                let! entityId = ownershipStorage.SaveEntity newEntity
                return { newEntity with Id = entityId }
    }

    let createOwnershipEvent (entity: OwnershipEntity) (parsed: ParsedSchedule13G) (filing: SECFilingRecord) (ticker: Ticker) = 
        // Determine event type based on whether this is an amendment
        let eventType = 
            if parsed.IsAmendment then 
                "beneficial_ownership_update" 
            else 
                "position_disclosure"
        
        // Use transaction date from parsed data, or filing date if not available
        let transactionDate = 
            match parsed.AsOfDate with
            | Some asOf -> asOf.ToString("yyyy-MM-dd")
            | None -> filing.FilingDate
        
        // Determine ownership nature from voting/dispositive powers
        let ownershipNature = 
            match parsed.SoleVotingPower, parsed.SharedVotingPower with
            | Some sole, Some shared when sole > 0L && shared > 0L -> Some "Sole and shared voting power"
            | Some sole, _ when sole > 0L -> Some "Sole voting power"
            | _, Some shared when shared > 0L -> Some "Shared voting power"
            | _ -> None

        OwnershipEvent.create
            entity.Id
            ticker.Value
            filing.Cik
            (Some filing.Id)
            eventType
            None // No transaction type for position disclosures
            None // No shares before
            None // No shares transacted
            parsed.SharesOwned
            (Some parsed.PercentOfClass)
            None // No price per share in 13G
            None // No total value in 13G
            transactionDate
            filing.FilingDate
            true // Assume direct ownership (13G typically is)
            ownershipNature

    let processSchedule13GFiling (filing: SECFilingRecord) = task {
        try
            logger.LogInformation($"Processing Schedule 13G filing for {filing.Ticker}: {filing.FilingUrl}")
            
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
                    // Parse the XML using Schedule13GParser
                    let parseResult = Schedule13GParser.parseFromDocument (Some logger) xmlContent
                    
                    match parseResult with
                    | Success parsed ->
                        logger.LogInformation($"Successfully parsed Schedule 13G for {filing.Ticker}: {parsed.FilerName}, {parsed.SharesOwned} shares ({parsed.PercentOfClass}%%)")
                        
                        // Find or create entity
                        let! entity = findOrCreateEntity parsed
                        
                        // Create ownership event
                        let ownershipEvent = createOwnershipEvent entity parsed filing ticker
                        
                        // Save the event
                        let! eventId = ownershipStorage.SaveEvent ownershipEvent
                        logger.LogInformation($"Created ownership event {eventId} for entity {entity.Name} and ticker {filing.Ticker}")
                        
                        return Ok ()
                        
                    | PartialSuccess (parsed, notes) ->
                        let notesStr = String.Join("; ", notes |> List.toArray)
                        logger.LogWarning($"Partial success parsing Schedule 13G for {filing.Ticker}. Confidence: {parsed.Confidence:F2}. Notes: {notesStr}")
                        
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
                    
                    | Failure msg ->
                        logger.LogError($"Failed to parse Schedule 13G for {filing.Ticker}: {msg}")
                        return Error $"Parse failure: {msg}"
                        
        with ex ->
            logger.LogError(ex, "Error processing Schedule 13G filing for {Ticker}: {Message}", filing.Ticker, ex.Message)
            return Error ex.Message
    }

    interface IApplicationService

    member _.Execute() = task {
        try
            logger.LogInformation("Starting Schedule 13G processing service")
            
            // Query for recent 13G and 13G/A filings (last 30 days)
            // Include both "SC 13G" and "SCHEDULE 13G" variations to handle different SEC API formats
            let formTypes = [| "SC 13G"; "SC 13G/A"; "SCHEDULE 13G"; "SCHEDULE 13G/A" |]
            let! recentFilings = secFilingStorage.GetFilingsByFormType formTypes 100
            
            let filings = 
                recentFilings
                |> Seq.filter (fun f -> isSchedule13G f.FormType)
                |> Seq.toArray
            
            logger.LogInformation($"Found {filings.Length} Schedule 13G/13G-A filings to process")
            
            if filings.Length = 0 then
                logger.LogInformation("No Schedule 13G filings to process")
            else
                // Process each filing
                let mutable successCount = 0
                let mutable failureCount = 0
                
                for filing in filings do
                    let! result = processSchedule13GFiling filing
                    match result with
                    | Ok _ -> successCount <- successCount + 1
                    | Error _ -> failureCount <- failureCount + 1
                    
                    // Add a small delay between processing to respect rate limits
                    do! System.Threading.Tasks.Task.Delay(500)
                
                logger.LogInformation($"Schedule 13G processing completed. Success: {successCount}, Failures: {failureCount}")
        
        with ex ->
            logger.LogError(ex, "Error in Schedule 13G processing service: {Message}", ex.Message)
    }

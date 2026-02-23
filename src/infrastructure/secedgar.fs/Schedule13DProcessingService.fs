namespace secedgar.fs

open System
open Microsoft.Extensions.Logging
open core.fs
open core.fs.Adapters.SEC
open core.fs.Adapters.Storage
open core.Shared

type Schedule13DProcessingService(
    secFilingStorage: ISECFilingStorage,
    ownershipStorage: IOwnershipStorage,
    secFilings: ISECFilings,
    logger: ILogger<Schedule13DProcessingService>) =

    let isSchedule13D (formType: string) =
        formType.Contains("13D", StringComparison.OrdinalIgnoreCase)

    let findOrCreateEntity (parsed: ParsedSchedule13D) = task {
        // Try to find entity by CIK first (most reliable)
        match parsed.FilerCik with
        | Some cik ->
            let! existingEntity = ownershipStorage.FindEntityByCik cik
            match existingEntity with
            | Some entity ->
                logger.LogInformation($"Found existing entity by CIK: {entity.Name} ({cik})")
                return entity
            | None ->
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
                let entityType = defaultArg parsed.EntityType "OO"
                let newEntity = OwnershipEntity.create parsed.FilerName entityType None
                logger.LogInformation($"Creating new entity without CIK: {parsed.FilerName}")
                let! entityId = ownershipStorage.SaveEntity newEntity
                return { newEntity with Id = entityId }
    }

    let createOwnershipEvent (entity: OwnershipEntity) (parsed: ParsedSchedule13D) (filing: SECFilingRecord) (ticker: Ticker) = 
        // 13D filers typically have activist or concentrated ownership intent -
        // use "large_stake_disclosure" for new filings, "beneficial_ownership_update" for amendments
        let eventType = 
            if parsed.IsAmendment then 
                "beneficial_ownership_update" 
            else 
                "large_stake_disclosure"
        
        let transactionDate = 
            match parsed.AsOfDate with
            | Some asOf -> asOf.ToString("yyyy-MM-dd")
            | None -> filing.FilingDate
        
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
            None // No specific transaction type for position disclosures
            None // No shares before
            None // No shares transacted
            (Some parsed.SharesOwned)
            (Some parsed.PercentOfClass)
            None // No price per share in 13D
            None // No total value in 13D
            transactionDate
            filing.FilingDate
            true // Assume direct ownership
            ownershipNature

    let processSchedule13DFiling (filing: SECFilingRecord) = task {
        try
            logger.LogInformation($"Processing Schedule 13D filing for {filing.Ticker}: {filing.FilingUrl}")
            
            // Check if this filing has already been processed
            let ticker = Ticker filing.Ticker
            let! existingEvents = ownershipStorage.GetEventsByFilingId filing.Id
            let alreadyProcessed =
                existingEvents
                |> Seq.isEmpty
                |> not
            
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
                    // Parse the XML using Schedule13DParser
                    let parseResult = Schedule13DParser.parseFromDocument (Some logger) xmlContent
                    
                    match parseResult with
                    | Schedule13DSuccess parsed ->
                        logger.LogInformation($"Successfully parsed Schedule 13D for {filing.Ticker}: {parsed.FilerName}, {parsed.SharesOwned} shares ({parsed.PercentOfClass}%%)")
                        
                        let! entity = findOrCreateEntity parsed
                        let ownershipEvent = createOwnershipEvent entity parsed filing ticker
                        let! eventId = ownershipStorage.SaveEvent ownershipEvent
                        logger.LogInformation($"Created ownership event {eventId} for entity {entity.Name} and ticker {filing.Ticker}")
                        
                        return Ok ()
                        
                    | Schedule13DPartialSuccess (parsed, notes) ->
                        let notesStr = String.Join("; ", notes |> List.toArray)
                        logger.LogWarning($"Partial success parsing Schedule 13D for {filing.Ticker}. Confidence: {parsed.Confidence:F2}. Notes: {notesStr}")
                        
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
                    
                    | Schedule13DFailure msg ->
                        logger.LogError($"Failed to parse Schedule 13D for {filing.Ticker}: {msg}")
                        return Error $"Parse failure: {msg}"
                        
        with ex ->
            logger.LogError(ex, "Error processing Schedule 13D filing for {Ticker}: {Message}", filing.Ticker, ex.Message)
            return Error ex.Message
    }

    interface IApplicationService

    member _.Execute() = task {
        try
            logger.LogInformation("Starting Schedule 13D processing service")
            
            let formTypes = [| "SC 13D"; "SC 13D/A"; "SCHEDULE 13D"; "SCHEDULE 13D/A" |]
            let! recentFilings = secFilingStorage.GetFilingsByFormType formTypes 100
            
            let filings = 
                recentFilings
                |> Seq.filter (fun f -> isSchedule13D f.FormType)
                |> Seq.toArray
            
            logger.LogInformation($"Found {filings.Length} Schedule 13D/13D-A filings to process")
            
            if filings.Length = 0 then
                logger.LogInformation("No Schedule 13D filings to process")
            else
                let mutable successCount = 0
                let mutable failureCount = 0
                
                for filing in filings do
                    let! result = processSchedule13DFiling filing
                    match result with
                    | Ok _ -> successCount <- successCount + 1
                    | Error _ -> failureCount <- failureCount + 1
                    
                    // Respect SEC rate limits
                    do! System.Threading.Tasks.Task.Delay(500)
                
                logger.LogInformation($"Schedule 13D processing completed. Success: {successCount}, Failures: {failureCount}")
        
        with ex ->
            logger.LogError(ex, "Error in Schedule 13D processing service: {Message}", ex.Message)
    }

module core.fs.Options.MonitoringServices

open core.Account
open core.fs
open core.fs.Accounts
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Logging
open core.fs.Adapters.Options
open core.fs.Adapters.Storage

type PriceMonitoringService(
    accounts: IAccountStorage,
    portfolio: IPortfolioStorage,
    brokerage: IBrokerage,
    logger: ILogger) =

    let fetchChain (user:UserState) (p:OptionPositionState) = async {
        try

            let! chainResult = brokerage.GetOptionChain user SkipCache p.UnderlyingTicker |> Async.AwaitTask
            match chainResult with
            | Error e ->
                let closedValue = match p.Closed with | Some d -> d.ToString() | None -> "N/A"
                logger.LogError $"Failed to get option chain for {p.UnderlyingTicker} for {p.Opened} to {closedValue}: {e}"
                return None
            | Ok chain ->
                return Some (p, chain)
        with ex ->
            logger.LogError $"Exception getting option chain for {p.UnderlyingTicker} opened on {p.Opened}: {ex}"
            return None
    }
    
    let matchDetailWithContract (p:OptionPositionState, chain:OptionChain) =
        try

            let matchDetailWithContract (p:OptionPositionState,c:OptionContract, chain:OptionChain) =
                let detail = chain.FindMatchingOption(c.Strike, c.Expiration, c.OptionType)
                match detail with
                | None ->
                    let closedValue = match p.Closed with | Some d -> d.ToString() | None -> "N/A"
                    logger.LogError $"Failed to find option contract for {c.Strike} {c.Expiration} {c.OptionType} in chain for {p.UnderlyingTicker} opened on {p.Opened} and closed {closedValue}"
                    None
                | Some detail ->
                    Some (p, c, detail)
                    
            let activeContracts =
                p.Contracts.Keys
                |> Seq.filter (fun c -> c.Expiration.ToDateTimeOffset() > System.DateTimeOffset.UtcNow)
                |> Seq.map( fun c -> matchDetailWithContract (p, c, chain))
                |> Seq.choose id
                
            let pendingContracts =
                p.PendingContracts.Keys
                |> Seq.filter (fun c -> c.Expiration.ToDateTimeOffset() > System.DateTimeOffset.UtcNow)
                |> Seq.map( fun c -> matchDetailWithContract (p, c, chain))
                |> Seq.choose id

            let closedContracts =
                p.ClosedContracts.Keys
                |> Seq.filter (fun c -> c.Expiration.ToDateTimeOffset() <= System.DateTimeOffset.UtcNow)
                |> Seq.map( fun c -> matchDetailWithContract (p, c, chain))
                |> Seq.choose id
                
            Seq.concat [activeContracts; pendingContracts; closedContracts]
        with ex ->
            let closedValue = match p.Closed with | Some d -> d.ToString() | None -> "N/A"
            logger.LogError $"Exception matching option details with contracts for position on {p.UnderlyingTicker} opened on {p.Opened} and closed {closedValue}: {ex}"
            Seq.empty
        
    let saveOptionPricing (userId:UserId) timestamp (position:OptionPositionState) (contract:OptionContract) (pricing:OptionDetail) = async {
        let pricing = {
            OptionPricing.Ask = pricing.Ask
            Bid = pricing.Bid
            Last = pricing.Last
            Mark = pricing.Mark
            OpenInterest = pricing.OpenInterest
            Volume = pricing.Volume
            Delta = pricing.Delta
            Gamma = pricing.Gamma
            Theta = pricing.Theta
            Vega = pricing.Vega
            Rho = pricing.Rho
            Volatility = pricing.Volatility
            Expiration = contract.Expiration
            Symbol = OptionTicker.create pricing.Symbol
            OptionType = contract.OptionType
            StrikePrice = contract.Strike
            UnderlyingTicker = position.UnderlyingTicker
            UnderlyingPrice = pricing.UnderlyingPrice
            UserId = userId
            OptionPositionId = position.PositionId
            Timestamp = timestamp
        }
            
        let! _ = accounts.SaveOptionPricing pricing userId |> Async.AwaitTask
        return pricing
    }
    
    interface IApplicationService
    
    member _.Run() = task {
        let! pairs = accounts.GetUserEmailIdPairs()
        
        let! _ =
            pairs
            |> Seq.map (fun pair -> async {
                let! user = pair.Id |> accounts.GetUser |> Async.AwaitTask
                match user with
                | None -> ()
                | Some user ->
                    
                    match user.State.ConnectedToBrokerage with
                    | false -> ()
                    | true ->
                        // I want that timestamp to represent the time of the pricing run and not change
                        // between saves as we go through the loop
                        // and also removing the milliseconds because it looks ridiculous in db, and doesn't really matter
                        let timestamp = System.DateTimeOffset.UtcNow
                        let timestamp = timestamp.AddMilliseconds -timestamp.Millisecond
                        
                        let! positions = pair.Id |> portfolio.GetOptionPositions |> Async.AwaitTask
                        
                        let closedPositionMonitoringThreshold = timestamp.AddDays -30.
                        
                        let positionsToMonitor =
                            positions
                            |> Seq.filter (fun p -> p.IsClosed |> not)
                            |> Seq.append (
                                positions
                                |> Seq.filter (fun p -> p.IsClosed && p.Closed.Value > closedPositionMonitoringThreshold)
                            )
                            |> Seq.filter (fun p -> p.HasNonExpiredContracts)
                        
                        // let's go out and get the latest price for each open position contracts
                        let! positionsWithChainOptions =
                            positionsToMonitor
                            |> Seq.map(fetchChain user.State)
                            |> Async.Sequential
                            
                        let! positionsWithContractPrices =
                            positionsWithChainOptions
                            |> Seq.choose id
                            |> Seq.collect matchDetailWithContract
                            |> Seq.map (fun (position,contract,optionDetail) -> saveOptionPricing pair.Id timestamp position contract optionDetail)
                            |> Async.Sequential
                            
                        logger.LogInformation($"Updated {positionsWithContractPrices.Length} option pricing records for {pair.Email}")
                            
                        ()
                        
            })
            |> Async.Sequential
        
        return ()
    }

type ExpirationMonitoringService(
    accounts: IAccountStorage,
    portfolio: IPortfolioStorage,
    logger: ILogger) =
    
    interface IApplicationService
    
    member _.Run() = task {
        logger.LogInformation("Starting option expiration monitoring")
        
        let! pairs = accounts.GetUserEmailIdPairs()
        let today = System.DateTimeOffset.UtcNow.Date |> System.DateTimeOffset
        
        let! _ =
            pairs
            |> Seq.map (fun pair -> async {
                try
                    let! positions = pair.Id |> portfolio.GetOptionPositions |> Async.AwaitTask
                    
                    // Find all open positions with contracts expiring today or earlier
                    let positionsWithExpiredContracts =
                        positions
                        |> Seq.filter (fun p -> p.IsOpen)
                        |> Seq.filter (fun p -> 
                            p.Contracts.Keys 
                            |> Seq.exists (fun c -> c.Expiration.ToDateTimeOffset().Date <= today.Date))
                        |> Seq.toList
                    
                    if positionsWithExpiredContracts.Length > 0 then
                        logger.LogInformation($"Found {positionsWithExpiredContracts.Length} positions with expired contracts for {pair.Email}")
                        
                        // Process each position with expired contracts
                        for position in positionsWithExpiredContracts do
                            try
                                let expiredContracts =
                                    position.Contracts.Keys
                                    |> Seq.filter (fun c -> c.Expiration.ToDateTimeOffset().Date <= today.Date)
                                    |> Seq.toList
                                
                                // Expire each contract using fold
                                let updatedPosition =
                                    expiredContracts
                                    |> List.fold (fun pos contract ->
                                        try
                                            logger.LogInformation($"Expiring contract {contract.Strike} {contract.OptionType} {contract.Expiration} for {position.UnderlyingTicker}")
                                            pos |> OptionPosition.expire contract.Expiration contract.Strike contract.OptionType
                                        with ex ->
                                            logger.LogError($"Error expiring contract {contract.Strike} {contract.OptionType} {contract.Expiration} for position {position.PositionId}: {ex.Message}")
                                            pos // Return unchanged position on error
                                    ) position
                                
                                // Save the updated position
                                do! portfolio.SaveOptionPosition pair.Id (Some position) updatedPosition |> Async.AwaitTask
                                logger.LogInformation($"Successfully expired {expiredContracts.Length} contracts for position on {position.UnderlyingTicker}")
                            with ex ->
                                logger.LogError($"Error processing expired contracts for position {position.PositionId}: {ex.Message}")
                    else
                        logger.LogInformation($"No expired contracts found for {pair.Email}")
                        
                with ex ->
                    logger.LogError($"Error processing expirations for user {pair.Email}: {ex.Message}")
            })
            |> Async.Sequential
        
        logger.LogInformation("Completed option expiration monitoring")
        return ()
    }

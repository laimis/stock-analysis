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
        let! chainResult = brokerage.GetOptionChain user p.UnderlyingTicker |> Async.AwaitTask
        match chainResult with
        | Error e ->
            logger.LogError($"Failed to get option chain for {p.UnderlyingTicker} for {p.Opened} to {p.Closed.Value}: {e.Message}")
            return None
        | Ok chain ->
            return Some (p, chain)
    }
    
    let matchDetailWithContract (p:OptionPositionState, chain:OptionChain) =
        p.Contracts.Keys
        |> Seq.map( fun c ->
            let detail = chain.FindMatchingOption(c.Strike, c.Expiration, c.OptionType)
            match detail with
            | None ->
                logger.LogError($"Failed to find option contract for {c.Strike} {c.Expiration} {c.OptionType} in chain for {p.UnderlyingTicker} for {p.Opened} to {p.Closed.Value}")
                None
            | Some detail ->
                Some (p,c,detail)
        )
        
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
                        let timestamp = timestamp.AddMilliseconds(-timestamp.Millisecond)
                        
                        let! positions = pair.Id |> portfolio.GetOptionPositions |> Async.AwaitTask
                        
                        let openPositions = positions |> Seq.filter _.IsOpen
                        
                        // let's go out and get the latest price for each open position contracts
                        let! positionsWithChainOptions =
                            openPositions
                            |> Seq.map(fetchChain user.State)
                            |> Async.Sequential
                            
                        let! positionsWithContractPrices =
                            positionsWithChainOptions
                            |> Seq.choose id
                            |> Seq.collect matchDetailWithContract
                            |> Seq.choose id
                            |> Seq.map (fun (position,contract,optionDetail) -> saveOptionPricing pair.Id timestamp position contract optionDetail)
                            |> Async.Sequential
                            
                        logger.LogInformation($"Updated {positionsWithContractPrices.Length} option pricing records for {pair.Email}")
                            
                        ()
                        
            })
            |> Async.Sequential
        
        return ()
    }

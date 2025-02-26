namespace core.fs.Options

open System
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Options
    
type OptionContractView(
    underlyingTicker:core.Shared.Ticker,
    expiration:OptionExpiration,
    strikePrice:decimal,
    optionType:OptionType,
    longOrShort:LongOrShort,
    quantity:int,
    cost:decimal,
    instruction:OptionOrderInstruction option,
    chain:OptionChain option) =
    let chainDetail = chain |> Option.bind(_.FindMatchingOption(strikePrice, expiration, optionType))
    let underlyingPrice = chain |> Option.bind(_.UnderlyingPrice)
    let pctItm =
        match underlyingPrice with
        | Some(price) ->
            
            let itmPrice = 
                match optionType with
                | Call -> price - strikePrice
                | Put -> strikePrice - price
            itmPrice / price |> Some
        | None -> None
    let market = chainDetail |> Option.map(_.Mark)
        
    member this.Expiration = expiration
    member this.StrikePrice = strikePrice
    member this.OptionType = optionType
    member this.Quantity = quantity
    member this.IsShort = longOrShort = Short
    member this.Cost = cost
    member this.Market = market
    member this.Details = chainDetail
    member this.PctInTheMoney = pctItm
    member this.UnderlyingTicker = underlyingTicker
    member this.UnderlyingPrice = underlyingPrice
    member this.Instruction = instruction
    member this.BrokerageSymbol =
        match chainDetail with
        | Some detail -> detail.Symbol
        | None ->
            let ticker = underlyingTicker.Value.PadRight(6)
            let date = expiration.ToDateTimeOffset()
            let dateStr = date.ToString("yyMMdd")
            let optionTypeStr = match optionType with | Call -> "C" | Put -> "P"
            let strikeStr = (strikePrice * 1000m |> int).ToString("00000000")
            $"{ticker}{dateStr}{optionTypeStr}{strikeStr}"
            
    
type OptionPositionView(state:OptionPositionState, chain:OptionChain option) =
    
    let labels = state.Labels |> Seq.map id |> Seq.toArray
    let contracts =
        match state.Contracts.Count with
        | 0 ->
            state.PendingContracts.Keys
            |> Seq.map (fun k ->
                let (PendingContractQuantity(longOrShort, quantity)) = state.PendingContracts[k]
                OptionContractView(state.UnderlyingTicker, k.Expiration, k.Strike, k.OptionType, longOrShort, quantity, 0m, None, chain)
            )
            |> Seq.toList
        | _ ->
            state.Contracts.Keys
            |> Seq.map (fun k ->
                let (OpenedContractQuantityAndCost(longOrShort, quantity, cost)) = state.Contracts[k]
                let perContractCost =
                    match quantity with
                    | 0 -> 0m
                    | _ -> cost / decimal quantity |> abs
                OptionContractView(state.UnderlyingTicker, k.Expiration, k.Strike, k.OptionType, longOrShort, quantity, perContractCost, None, chain)
            )
            |> Seq.toList
        
    member this.PositionId = state.PositionId
    member this.UnderlyingTicker = state.UnderlyingTicker
    member this.UnderlyingPrice =
        match contracts with
        | [] -> None
        | _ -> contracts[0].UnderlyingPrice
    member this.Opened = state.Opened
    member this.DaysHeld = state.DaysHeld
    member this.DaysToExpiration =
        // days are countedfrom the time it's opened
        let referenceDate = 
            match state.IsOpen && state.IsClosed with
            | true -> state.Opened.Value
            | false -> DateTimeOffset.Now

        contracts
        |> Seq.map (fun c -> c.Expiration.ToDateTimeOffset() - referenceDate)
        |> Seq.map (fun ts -> ts.TotalDays |> int)
        |> Seq.distinct
        |> Seq.sort
        
    member this.Closed = state.Closed
    member this.IsClosed = state.IsClosed
    member this.IsOpen = state.IsOpen
    member this.IsPending = state.IsPending
    member this.IsPendingClosed = state.IsPendingClosed
    member this.Cost = match state.Cost with Some c -> c | None -> state.DesiredCost |> Option.defaultValue 0m
    member this.Market =
        contracts
        |> Seq.sumBy (fun c -> c.Details |> Option.map(fun o -> o.Mark * decimal c.Quantity) |> Option.defaultValue 0m)
    member this.Spread =
        match contracts with
        | [] -> 0m
        | _ ->
            // get min and max values of the contract strike prices
            let minStrike = contracts |> Seq.map _.StrikePrice |> Seq.min
            let maxStrike = contracts |> Seq.map _.StrikePrice |> Seq.max
            maxStrike - minStrike
    member this.Risked =
        // for spreads, the risk is the cost of the spread
        match contracts with
        | x when x.Length > 1 ->
            match this.Cost with
            | x when x < 0m -> this.Spread + this.Cost
            | _ -> this.Spread - this.Cost
        | x when x.Length = 1 -> 
            let contract = x[0]
            match contract.IsShort with
            | true -> 
                match contract.OptionType with
                | Call -> Double.PositiveInfinity |> decimal
                | Put -> contract.StrikePrice
            | false -> this.Cost
        | _ -> 0m
        
    member this.Profit =
        match this.IsClosed with
        | true -> state.Profit
        | false -> this.Market - this.Cost
    member this.Transactions = state.Transactions
    member this.Notes = state.Notes
    member this.Labels = labels
    member this.Contracts = contracts
    

type OptionPositionStats(summaries:seq<OptionPositionView>) =
    
    let optionTrades = summaries |> Seq.toList
    let wins = optionTrades |> List.filter (fun s -> s.Profit >= 0m)
    let losses = optionTrades |> List.filter (fun s -> s.Profit < 0m)
    
    member this.Count = optionTrades |> List.length
    member this.Assigned = None // TODO: implement
    member this.AveragePremiumCapture = None // TODO: implement
    
    member this.Wins = wins |> List.length
    member this.AvgWinAmount =
        match wins with
        | [] -> None
        | _ -> wins |> List.averageBy (fun s -> s.Profit) |> Some
        
    member this.MaxWinAmount =
        match wins with
        | [] -> None
        | _ -> wins |> List.map (fun s -> s.Profit) |> List.max |> Some
        
    member this.Losses = losses |> List.length
    member this.AverageLossAmount =
        match losses with
        | [] -> None
        | _ -> losses |> List.averageBy (fun s -> s.Profit) |> abs |> Some
        
    member this.MaxLossAmount =
        match losses with
        | [] -> None
        | _ -> losses |> List.map (fun s -> s.Profit) |> List.min |> abs |> Some
    
    member this.EV =
        match (this.AvgWinAmount, this.AverageLossAmount) with
        | Some avgWinAmount, Some avgLossAmount ->
            let winPart = (avgWinAmount * decimal this.Wins / decimal this.Count)
            let lossPart = (avgLossAmount * decimal this.Losses / decimal this.Count)
            winPart - lossPart |> Some
        | _ -> None
            
    member this.AverageProfitPerDay =
        match optionTrades with
        | [] -> 0m
        | _ ->
            optionTrades
            |> List.map (fun s -> s.Profit / decimal (s.DaysHeld |> Option.defaultValue 0)) |> List.average
        
    member this.AverageDays =
        match optionTrades with
        | [] -> 0m
        | _ -> optionTrades |> List.map (fun s -> decimal (s.DaysToExpiration |> Seq.head)) |> List.average
    member this.AverageDaysHeld =
        match optionTrades with
        | [] -> 0m
        | _ -> optionTrades |> List.map (fun s -> decimal (s.DaysHeld |> Option.defaultValue 0)) |> List.average
    member this.AverageDaysHeldPercentage =
        match optionTrades with
        | [] -> 0m
        | _ ->
            optionTrades
            |> List.map (fun s ->
                let daysHeld = s.DaysHeld |> Option.defaultValue 0
                let daysToExpiration = s.DaysToExpiration |> Seq.head
                match daysToExpiration with
                | 0 -> 0m
                | _ -> decimal daysHeld / decimal daysToExpiration
            )
            |> List.average
    
type OptionOrderView(order:OptionOrder, chain:OptionChain option) =
    member this.OrderId = order.OrderId
    member this.Price = order.Price
    member this.Quantity = order.Quantity
    member this.Status = order.Status
    member this.Type = order.Type
    member this.ExecutionTime = order.ExecutionTime
    member this.EnteredTime = order.EnteredTime
    member this.ExpirationTime = order.ExpirationTime
    member this.CanBeCancelled = order.CanBeCancelled
    member this.CanBeRecorded = order.CanBeRecorded
    member this.IsActive = order.IsActive
    member this.Contracts =
        order.Contracts |> Seq.map (fun l ->
            let longOrShort = if l.Quantity > 0 then Long else Short
            OptionContractView(l.UnderlyingTicker, l.Expiration, l.StrikePrice, l.OptionType, longOrShort, l.Quantity, l.Price |> Option.defaultValue 0m, l.Instruction |> Some, chain)
        )
    
type OptionDashboardView(pending:seq<OptionPositionView>,``open``:seq<OptionPositionView>,closed:seq<OptionPositionView>, brokeragePositions:seq<BrokerageOptionPosition>, orders:seq<OptionOrderView>) =
    
    member this.Closed = closed
    member this.Open = ``open``
    member this.Pending = pending
    member this.Orders = orders
    member this.BrokeragePositions = brokeragePositions
    member this.OverallStats = OptionPositionStats(closed) // TODO make stats to incorporate the new option position structure
    member this.BuyStats = OptionPositionStats([])
    member this.SellStats = OptionPositionStats([])

type OptionChainView(chain:OptionChain) =
    
    member this.StockPrice = chain.UnderlyingPrice
    member this.Options = chain.Options
    member this.Expirations = chain.Options |> Seq.map _.Expiration |> Seq.distinct |> Seq.toArray
    member this.Volatility = chain.Volatility
    member this.NumberOfContracts = chain.NumberOfContracts

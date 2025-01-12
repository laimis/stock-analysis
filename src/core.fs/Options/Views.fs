namespace core.fs.Options

open System
open core.Options
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Options
    
type OptionPositionView(state:OptionPositionState, chain:OptionChain option) =
    
    let labels = state.Labels |> Seq.map id |> Seq.toArray
    let contracts =
        state.Contracts.Keys
        |> Seq.map (fun k ->
            let chainDetail = chain |> Option.bind(_.FindMatchingOption(k.Strike, k.Expiration, k.OptionType))
            let (QuantityAndCost(quantity, cost)) = state.Contracts[k]
            let underlyingPrice = chain |> Option.bind(_.UnderlyingPrice)
            let pctItm =
                match underlyingPrice with
                | Some(price) -> 
                    let itmPrice = 
                        match k.OptionType with
                        | Call -> price - k.Strike
                        | Put -> k.Strike - price
                    itmPrice / price |> Some
                | None -> None
            {|expiration = k.Expiration; strikePrice = k.Strike; optionType = k.OptionType; quantity = quantity; cost = cost; details = chainDetail; pctInTheMoney = pctItm|}
        )
    
    member this.PositionId = state.PositionId
    member this.UnderlyingTicker = state.UnderlyingTicker
    member this.Opened = state.Opened
    member this.DaysHeld = state.DaysHeld
    member this.DaysToExpiration =
        contracts
        |> Seq.map (fun c -> c.expiration.ToDateTimeOffset() - DateTimeOffset.Now)
        |> Seq.map (fun ts -> ts.TotalDays |> int)
        |> Seq.distinct
        |> Seq.sort
        
    member this.Closed = state.Closed
    member this.IsClosed = state.IsClosed
    member this.IsOpen = state.IsOpen
    member this.Cost = state.Cost
    member this.Market =
        contracts
        |> Seq.sumBy (fun c -> c.details |> Option.map(fun o -> o.Mark * decimal c.quantity) |> Option.defaultValue 0m)
    member this.Spread =
        // get min and max values of the contract strike prices
        let minStrike = contracts |> Seq.map (fun c -> c.strikePrice) |> Seq.min
        let maxStrike = contracts |> Seq.map (fun c -> c.strikePrice) |> Seq.max
        maxStrike - minStrike
    member this.Profit =
        match this.IsClosed with
        | true -> state.Profit
        | false -> this.Market - this.Cost
    member this.Transactions = state.Transactions
    member this.Notes = state.Notes
    member this.Labels = labels
    member this.Contracts = contracts
    
    
type OwnedOptionView(state:OwnedOptionState, optionDetail:OptionDetail option) =
    
    let getItmOtmLabel (currentPrice:decimal) (optionType:string) (strikePrice:decimal) =
        match optionType with
        | "CALL" -> 
            if currentPrice > strikePrice then "ITM"
            elif currentPrice = strikePrice then "ATM"
            else "OTM"
        | "PUT" -> 
            if currentPrice > strikePrice then "OTM"
            elif currentPrice = strikePrice then "ATM"
            else "ITM"
        | _ -> ""
        
    let isFavorable (boughtOrSold:string) (itmOtmLabel:string) =
        match boughtOrSold with
        | "Bought" -> itmOtmLabel <> "OTM"
        | "Sold" -> itmOtmLabel = "OTM"
        | _ -> false
        
    member this.Id = state.Id
    member this.Ticker = state.Ticker
    member this.OptionType = state.OptionType.ToString()
    member this.StrikePrice = state.StrikePrice
    member this.ExpirationDate = state.ExpirationDate
    member this.NumberOfContracts = abs state.NumberOfContracts
    member this.BoughtOrSold = if state.SoldToOpen.Value then "Sold" else "Bought"
    member this.Filled = state.FirstFill.Value
    member this.Days = state.Days
    member this.DaysHeld = state.DaysHeld
    member this.Transactions = state.Transactions |> Seq.filter (fun t -> not t.IsPL)
    member this.ExpiresSoon = state.ExpiresSoon
    member this.IsExpired = state.IsExpired
    member this.Closed = state.Closed
    member this.Assigned = state.Assigned
    member this.Notes = state.Notes
    member this.Detail = optionDetail
    member this.PremiumReceived = state.Transactions |> Seq.filter (fun t -> not t.IsPL && t.Amount >= 0m) |> Seq.sumBy (fun t -> t.Amount)
    member this.PremiumPaid = state.Transactions |> Seq.filter (fun t -> not t.IsPL && t.Amount < 0m) |> Seq.sumBy (fun t -> abs t.Amount)
    member this.CurrentPrice = if optionDetail.IsSome && optionDetail.Value.UnderlyingPrice.IsSome then optionDetail.Value.UnderlyingPrice.Value else 0m
    member this.ItmOtmLabel = if optionDetail.IsSome && optionDetail.Value.UnderlyingPrice.IsSome then getItmOtmLabel this.CurrentPrice this.OptionType this.StrikePrice else ""
    member this.IsFavorable = if optionDetail.IsSome && optionDetail.Value.UnderlyingPrice.IsSome then isFavorable this.BoughtOrSold this.ItmOtmLabel else false
    member this.StrikePriceDiff = if this.CurrentPrice > 0m then abs (this.StrikePrice - this.CurrentPrice) / this.CurrentPrice else 0m
    member this.PremiumCapture =
        if this.BoughtOrSold = "Bought" then
            (this.PremiumReceived - this.PremiumPaid) / this.PremiumPaid
        else
            (this.PremiumReceived - this.PremiumPaid) / this.PremiumReceived
    member this.Profit = this.PremiumReceived - this.PremiumPaid
    

type OwnedOptionStats(summaries:seq<OwnedOptionView>) =
    
    let optionTrades = summaries |> Seq.toList
    let wins = optionTrades |> List.filter (fun s -> s.Profit >= 0m)
    let losses = optionTrades |> List.filter (fun s -> s.Profit < 0m)
    
    member this.Count = optionTrades |> List.length
    member this.Assigned = optionTrades |> List.filter (fun s -> s.Assigned) |> List.length
    member this.AveragePremiumCapture =
        match optionTrades with
        | [] -> 0m
        | _ -> optionTrades |> List.averageBy _.PremiumCapture
    
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
        | _ -> optionTrades |> List.map (fun s -> s.Profit / decimal s.DaysHeld) |> List.average
        
    member this.AverageDays =
        match optionTrades with
        | [] -> 0m
        | _ -> optionTrades |> List.map (fun s -> decimal s.Days) |> List.average
    member this.AverageDaysHeld =
        match optionTrades with
        | [] -> 0m
        | _ -> optionTrades |> List.map (fun s -> decimal s.DaysHeld) |> List.average
    member this.AverageDaysHeldPercentage =
        match this.AverageDaysHeld with
        | 0m -> 0m
        | _ -> this.AverageDaysHeld / this.AverageDays
    
type OptionDashboardView(closed:seq<OptionPositionView>, ``open``:seq<OptionPositionView>, brokeragePositions:seq<BrokerageOptionPosition>, orders:seq<OptionOrder>) =
    
    member this.Closed = closed
    member this.Open = ``open``
    member this.Orders = orders
    member this.BrokeragePositions = brokeragePositions
    member this.OverallStats = OwnedOptionStats([]) // TODO make stats to incorporate the new option position structure
    member this.BuyStats = OwnedOptionStats([])
    member this.SellStats = OwnedOptionStats([])

type OptionChainView(chain:OptionChain) =
    
    member this.StockPrice = chain.UnderlyingPrice
    member this.Options = chain.Options
    member this.Expirations = chain.Options |> Seq.map _.Expiration |> Seq.distinct |> Seq.toArray
    member this.Volatility = chain.Volatility
    member this.NumberOfContracts = chain.NumberOfContracts

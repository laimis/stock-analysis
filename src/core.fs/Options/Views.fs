namespace core.fs.Options

open System
open core.Adapters.Options
open core.Options
open core.Shared.Adapters.Brokerage


type OwnedOptionView(state:OwnedOptionState, optionDetail:OptionDetail) =
    
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
    member this.CurrentPrice = if optionDetail = null |> not && optionDetail.UnderlyingPrice.HasValue then optionDetail.UnderlyingPrice.Value else 0m
    member this.ItmOtmLabel = if optionDetail = null |> not && optionDetail.UnderlyingPrice.HasValue then getItmOtmLabel this.CurrentPrice this.OptionType this.StrikePrice else ""
    member this.IsFavorable = if optionDetail = null |> not && optionDetail.UnderlyingPrice.HasValue then isFavorable this.BoughtOrSold this.ItmOtmLabel else false
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
    member this.AveragePremiumCapture = optionTrades |> List.averageBy (fun s -> s.PremiumCapture)
    
    member this.Wins = wins |> List.length
    member this.AvgWinAmount = wins |> List.averageBy (fun s -> s.Profit)
    member this.MaxWinAmount = wins |> List.maxBy (fun s -> s.Profit)
    member this.Losses = losses |> List.length
    member this.AverageLossAmount = losses |> List.averageBy (fun s -> s.Profit) |> abs
    member this.MaxLossAmount = losses |> List.map (fun s -> s.Profit) |> List.min |> abs
    
    member this.EV = (this.AvgWinAmount * decimal this.Wins / decimal this.Count) - (this.AverageLossAmount * decimal this.Losses / decimal this.Count)
    member this.AverageProfitPerDay = optionTrades |> List.averageBy (fun s -> s.Profit / decimal s.DaysHeld)
    member this.AverageDays = optionTrades |> List.averageBy (fun s -> decimal s.Days)
    member this.AverageDaysHeld = optionTrades |> List.averageBy (fun s -> decimal s.DaysHeld)
    member this.AverageDaysHeldPercentage = this.AverageDaysHeld / this.AverageDays
    
type OptionDashboardView(closed:seq<OwnedOptionView>, ``open``:seq<OwnedOptionView>, brokeragePositions:seq<OptionPosition>, orders:seq<Order>) =
    
    member this.Closed = closed
    member this.Open = ``open``
    member this.Orders = orders
    member this.BrokeragePositions = brokeragePositions
    member this.OverallStats = new OwnedOptionStats(closed)
    member this.BuyStats = new OwnedOptionStats(closed |> Seq.filter (fun s -> s.BoughtOrSold = "Bought"))
    member this.SellStats = new OwnedOptionStats(closed |> Seq.filter (fun s -> s.BoughtOrSold = "Sold"))

type OptionDetailsViewModel(price:Nullable<decimal>, chain:OptionChain) =
    
    member this.StockPrice = price
    member this.Options = chain.Options
    member this.Expirations = chain.Options |> Seq.map (fun o -> o.ExpirationDate) |> Seq.distinct |> Seq.toArray
    member this.Volatility = chain.Volatility
    member this.NumberOfContracts = chain.NumberOfContracts
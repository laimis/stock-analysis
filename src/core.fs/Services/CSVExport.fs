namespace core.fs.Services

open System
open core.Cryptos
open core.Options
open core.Shared
open core.Stocks
open core.fs.Accounts
open core.fs.Adapters.CSV
open core.fs.Services.Trading
open core.fs.Stocks

module CSVExport =
    
    [<Literal>]
    let private DATE_FORMAT = "yyyy-MM-dd"
    [<Literal>]
    let private CURRENCY_FORMAT = "C"
    [<Literal>]
    let private PERCENT_FORMAT = "P"
    [<Literal>]
    let private NUMBER_FORMAT = "N2"
    
    let private number culture (value:decimal) = value.ToString(NUMBER_FORMAT, culture)
    let private numberOption culture (opt:decimal option) = opt |> Option.map (number culture) |> Option.defaultValue ""
    let private currency culture (value:decimal) = value.ToString(CURRENCY_FORMAT, culture)
    let private percent culture (value:decimal) = value.ToString(PERCENT_FORMAT, culture)
    let private currencyOption culture (opt:decimal option) = opt |> Option.map (currency culture) |> Option.defaultValue ""
    let private date (value:DateTimeOffset) = value.ToString(DATE_FORMAT)
    let private dateOption (opt:DateTimeOffset option) = opt |> Option.map date |> Option.defaultValue ""
    
    type PendingPositionRecord =
        {
            Ticker:string
            Bid:decimal
            NumberOfShares:decimal
            StopPrice:string
            Created:string
            Closed:string
            Purchased:bool
            Strategy:string
            CloseReason:string
            OrderType:string
            OrderDuration:string
            Notes:string
        }
        
    type StockRecord =
        {
            Ticker:string
            Type:string
            Amount:decimal
            Price:decimal
            Date:string
        }
        
    type NoteRecord =
        {
            Created:string
            Ticker:string
            Note:string
        }
        
    type OptionRecord =
        {
            Ticker:string
            Type:string
            Strike:decimal
            OptionType:string
            Expiration:string
            Amount:decimal
            Premium:decimal
            Filled:string
        }
        
    type UserRecord =
        {
            Email:string
            Firstname:string
            Lastname:string
        }
        
    type CryptosRecord =
        {
            Symbol:string
            Type:string
            Amount:decimal
            Price:decimal
            Date:string
        }
    
    type TradesRecord =
        {
            StrategyName:string
            Ticker:string
            PositionType:string
            NumberOfShares:string
            Opened:string
            Closed:string
            DaysHeld:int
            AverageCostPerShare:string
            LastSellCostPerShare:string
            AverageBuyCostPerShare:string
            AverageSaleCostPerShare:string
            CompletedPositionCostPerShare:string
            StopPrice:string
            Cost:string
            Profit:string
            ReturnPct:string
            RR:string
            InitialRiskedAmount:string
            RiskAmount:string
            MaxDrawdown:string
            MaxGain:string
            Strategy:string
            Grade:string
            GradeNote:string
        }
        
    type StockListRecord =
        {
            Ticker:string
            Created:string
            Notes:string
        }
        
    type StockListRecordJustTicker =
        {
            Ticker:string
        }
        
    let private mapToTradeRecord culture strategyName maxDrawdown maxGain (t:StockPositionWithCalculations) =
        {
            StrategyName = strategyName
            Ticker = t.Ticker.Value
            PositionType = t.StockPositionType
            NumberOfShares = (if t.IsClosed then t.CompletedPositionShares else t.NumberOfShares) |> number culture
            Opened = t.Opened |> date
            Closed = t.Closed |> dateOption
            DaysHeld = t.DaysHeld
            AverageBuyCostPerShare = t.AverageBuyCostPerShare |> number culture
            AverageSaleCostPerShare = t.AverageSaleCostPerShare |> number culture 
            AverageCostPerShare = t.CompletedPositionCostPerShare |> number culture
            LastSellCostPerShare = t.ClosePrice |> numberOption culture
            CompletedPositionCostPerShare = t.CompletedPositionCostPerShare |> number culture
            StopPrice = t.StopPrice  |> numberOption culture
            Cost = t.Cost |> number culture
            Profit = t.Profit |> number culture
            ReturnPct = t.GainPct |> percent culture
            RR = t.RR |> number culture
            InitialRiskedAmount = t.InitialRiskedAmount |> numberOption culture
            RiskAmount = t.RiskedAmount |> numberOption culture
            MaxDrawdown = maxDrawdown |> numberOption culture
            MaxGain = maxGain |> numberOption culture
            Strategy = t.TryGetLabelValue("strategy") |> Option.defaultValue ""
            Grade = if t.Grade.IsSome then t.Grade.Value.Value else ""
            GradeNote = t.GradeNote |> Option.defaultValue ""
        }
        
    let strategyPerformance (culture:IFormatProvider) (writer:ICSVWriter) (strategies:seq<TradingStrategyPerformance>) =
        
        let rows =
            strategies
            |> Seq.collect (fun strategy -> 
                strategy.results
                |> Seq.map (fun result ->
                    let maxDrawdown = result.MaxDrawdownPct |> Some
                    let maxGain = result.MaxGainPct |> Some
                    mapToTradeRecord culture strategy.strategyName maxDrawdown maxGain result.Position)
            )
            
        writer.Generate(rows)
    
    let pendingPositions (culture:IFormatProvider) (writer:ICSVWriter) (pending:seq<PendingStockPosition>) =
        
        let rows =
            pending
            |> Seq.map (fun p ->
                {
                    Ticker = p.State.Ticker.Value
                    Bid = p.State.Bid
                    NumberOfShares = p.State.NumberOfShares
                    StopPrice = p.State.StopPrice |> numberOption culture
                    Created = p.State.Created |> date
                    Closed = p.State.Closed |> dateOption
                    Purchased = p.State.Purchased
                    Strategy = p.State.Strategy
                    Notes = p.State.Notes
                    CloseReason = p.State.CloseReason
                    OrderDuration = p.State.OrderDuration
                    OrderType = p.State.OrderType 
                }
            )
            
        writer.Generate(rows)
        
    let stockList (writer:ICSVWriter) (list:StockListState) (justTickers:bool) =
        
        match justTickers with
        | true -> writer.Generate(list.Tickers |> Seq.map (fun s -> { Ticker = s.Ticker.Value }))
        | false -> writer.Generate(list.Tickers |> Seq.map (fun s -> { Ticker = s.Ticker.Value; Created = s.When |> date; Notes = s.Note }))
        
    
    let trades culture (writer:ICSVWriter) (trades:seq<StockPositionWithCalculations>) =
        
        let rows =
            trades
            |> Seq.map (mapToTradeRecord culture "Actual Trade" None None)
            
        writer.Generate(rows)
        
    let users (writer:ICSVWriter) (users:seq<User>) =
        
        let rows =
            users
            |> Seq.map (fun u ->
                {
                    Email = u.State.Email
                    Firstname = u.State.Firstname
                    Lastname = u.State.Lastname
                }
            )
            
        writer.Generate(rows)
        
    let cryptos (writer:ICSVWriter) (cryptos:seq<OwnedCrypto>) =
        
        let rows =
            cryptos
            |> Seq.collect (fun o -> o.State.UndeletedBuysOrSells |> Seq.map (fun e -> (o, e)))
            |> Seq.sortBy (fun (_, e) -> e.When)
            |> Seq.map (fun (o, e) ->
                match e with
                | :? CryptoPurchased as cp -> 
                    {
                        Symbol = o.State.Token
                        Type = "buy"
                        Amount = cp.Quantity
                        Price = cp.DollarAmount
                        Date = cp.When |> date
                    }
                | :? CryptoSold as cs ->
                    {
                        Symbol = o.State.Token
                        Type = "sell"
                        Amount = cs.Quantity
                        Price = cs.DollarAmount
                        Date = cs.When |> date
                    }
                | _ -> { Symbol = ""; Type = ""; Amount = 0m; Price = 0m; Date = "" }
            )
            |> Seq.filter (fun r -> r.Symbol <> "")
            
        writer.Generate(rows)
        
    let stocks (writer:ICSVWriter) (stocks:seq<StockPositionState>) =
        
        let rows =
            stocks
            |> Seq.collect (fun o -> o.ShareTransactions |> Seq.map (fun e -> (o, e)))
            |> Seq.sortBy (fun (_, e) -> e.Date)
            |> Seq.map (fun (o, e) ->
                {
                    Ticker = o.Ticker.Value
                    Type = e.Type.ToString().ToLower()
                    Amount = e.NumberOfShares
                    Price = e.Price
                    Date = e.Date |> date
                }
            )
            |> Seq.filter (fun r -> r.Ticker <> "")
            
        writer.Generate(rows)
        
    let options (writer:ICSVWriter) (options:seq<OwnedOption>) =
        
        let buys = options |> Seq.collect (fun o -> o.State.Buys |> Seq.map (fun e -> (o, e :> AggregateEvent)))
        let sells = options |> Seq.collect (fun o -> o.State.Sells |> Seq.map (fun e -> (o, e :> AggregateEvent)))
        let expirations = options |> Seq.collect (fun o -> o.State.Expirations |> Seq.map (fun e -> (o, e :> AggregateEvent)))
        
        let data = buys |> Seq.append sells |> Seq.append expirations
        
        
            
        let rows =
            data
            |> Seq.sortBy (fun (_, e) -> e.When)
            |> Seq.map (fun (o, e) -> 
                match e with
                | :? OptionSold as os ->
                    {
                        Ticker = o.State.Ticker.Value
                        Type = "sell"
                        Strike = o.State.StrikePrice
                        OptionType = o.State.OptionType.ToString()
                        Expiration = o.State.Expiration |> date
                        Amount = os.NumberOfContracts |> decimal
                        Premium = os.Premium
                        Filled = os.When |> date
                    }
                | :? OptionPurchased as op ->
                    {
                        Ticker = o.State.Ticker.Value
                        Type = "buy"
                        Strike = o.State.StrikePrice
                        OptionType = o.State.OptionType.ToString()
                        Expiration = o.State.Expiration |> date
                        Amount = op.NumberOfContracts |> decimal
                        Premium = op.Premium
                        Filled = op.When |> date
                    }
                | :? OptionExpired as oe ->
                    {
                        Ticker = o.State.Ticker.Value
                        Type = (if oe.Assigned then "assigned" else "expired")
                        Strike = o.State.StrikePrice
                        OptionType = o.State.OptionType.ToString()
                        Expiration = o.State.Expiration |> date
                        Amount = 0m
                        Premium = 0m
                        Filled = oe.When |> date
                    }
                | _ -> { Ticker = ""; Type = ""; Strike = 0m; OptionType = ""; Expiration = ""; Amount = 0m; Premium = 0m; Filled = "" }
            )
            |> Seq.filter (fun r -> r.Ticker <> "")
            
        writer.Generate(rows)


    let generateFilename prefix =
        prefix + "_" + DateTime.UtcNow.ToString("yyyyMMdd_hhmss") + ".csv"



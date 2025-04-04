namespace core.fs.Services

open System
open core.Cryptos
open core.Stocks
open core.fs.Accounts
open core.fs.Adapters.CSV
open core.fs.Options
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
            MaxDrawdownFirst10Bars:string
            MaxGainFirst10Bars:string
            Strategy:string
            Grade:string
            EntryNote:string
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
        
    let private mapToTradeRecord culture strategyName maxDrawdown maxGain maxDrawdownFirst10Bars maxGainFirst10Bars (t:StockPositionWithCalculations) =
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
            MaxDrawdownFirst10Bars = maxDrawdownFirst10Bars |> numberOption culture
            MaxGainFirst10Bars = maxGainFirst10Bars |> numberOption culture
            Strategy = t.TryGetLabelValue("strategy") |> Option.defaultValue ""
            Grade = if t.Grade.IsSome then t.Grade.Value.Value else ""
            GradeNote = t.GradeNote |> Option.defaultValue ""
            EntryNote = t.Notes |> List.tryHead |> Option.map(_.content) |> Option.defaultValue ""
        }
        
    let strategyPerformance (culture:IFormatProvider) (writer:ICSVWriter) (strategies:seq<TradingStrategyPerformance>) =
        
        let rows =
            strategies
            |> Seq.collect (fun strategy -> 
                strategy.results
                |> Seq.map (fun result ->
                    let maxDrawdown = result.MaxDrawdownPct |> Some
                    let maxGain = result.MaxGainPct |> Some
                    let maxDrawdownFirst10Bars = result.MaxDrawdownFirst10Bars |> Some
                    let maxGainFirst10Bars = result.MaxGainFirst10Bars |> Some
                    mapToTradeRecord culture strategy.strategyName maxDrawdown maxGain maxDrawdownFirst10Bars maxGainFirst10Bars result.Position)
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
            |> Seq.map (fun p ->
                let mae = p.TryGetLabelValue("mae") |> Option.map decimal
                let mfe = p.TryGetLabelValue("mfe") |> Option.map decimal
                let maeFirst10Bars = p.TryGetLabelValue("mae10") |> Option.map decimal
                let mfeFirst10Bars = p.TryGetLabelValue("mfe10") |> Option.map decimal
                
                mapToTradeRecord culture "Actual Trade" mae mfe maeFirst10Bars mfeFirst10Bars p
            )
            
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
        
    let options (writer:ICSVWriter) (options:seq<OptionPositionState>) =
        
        let data = options |> Seq.collect (fun o -> o.Events |> Seq.map (fun e -> (o, e)))
            
        let rows =
            data
            |> Seq.sortBy (fun (_, e) -> e.When)
            |> Seq.map (fun (o, e) -> 
                match e with
                | :? OptionContractSoldToOpen as os ->
                    {
                        Ticker = o.UnderlyingTicker.Value
                        Type = "sell"
                        Strike = os.Strike
                        OptionType = os.OptionType
                        Expiration = os.Expiration
                        Amount = os.Quantity |> decimal
                        Premium = os.Price
                        Filled = os.When |> date
                    }
                | :? OptionContractBoughtToOpen as op ->
                    {
                        Ticker = o.UnderlyingTicker.Value
                        Type = "buy"
                        Strike = op.Strike
                        OptionType = op.OptionType
                        Expiration = op.Expiration
                        Amount = op.Quantity |> decimal
                        Premium = op.Price
                        Filled = op.When |> date
                    }
                | :? OptionContractBoughtToClose as bc ->
                    {
                        Ticker = o.UnderlyingTicker.Value
                        Type = "buy"
                        Strike = bc.Strike
                        OptionType = bc.OptionType
                        Expiration = bc.Expiration
                        Amount = bc.Quantity |> decimal
                        Premium = bc.Price
                        Filled = bc.When |> date
                    }
                | :? OptionContractSoldToClose as sc ->
                    {
                        Ticker = o.UnderlyingTicker.Value
                        Type = "sell"
                        Strike = sc.Strike
                        OptionType = sc.OptionType
                        Expiration = sc.Expiration
                        Amount = sc.Quantity |> decimal
                        Premium = sc.Price
                        Filled = sc.When |> date
                    }
                | _ -> { Ticker = ""; Type = ""; Strike = 0m; OptionType = ""; Expiration = ""; Amount = 0m; Premium = 0m; Filled = "" }
            )
            |> Seq.filter (fun r -> r.Ticker <> "")
            
        writer.Generate(rows)


    let generateFilename prefix =
        prefix + "_" + DateTime.UtcNow.ToString("yyyyMMdd_hhmss") + ".csv"



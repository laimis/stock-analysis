namespace core.fs.Services

open core.Cryptos
open core.Options
open core.Shared
open core.Stocks
open core.fs.Accounts
open core.fs.Adapters.CSV
open core.fs.Services.Trading
open core.fs.Stocks

module CSVExport =
    
    let private DATE_FORMAT = "yyyy-MM-dd"
    let private CURRENCY_FORMAT = "C"
    let private PERCENT_FORMAT = "P"
    let private NUMBER_FORMAT = "G2"
    
    type PendingPositionRecord =
        {
            Ticker:string
            Bid:decimal
            NumberOfShares:decimal
            StopPrice:decimal option
            Date:string
            Closed:string option
            Purchased:bool
            Strategy:string
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
            Symbol:string
            PositionType:string
            NumberOfShares:string
            Opened:string
            Closed:string
            DaysHeld:int
            AverageCostPerShare:string
            LastSellCostPerShare:string
            Cost:string
            Profit:string
            ReturnPct:string
            RR:string
            RiskedAmount:string
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
        
    type TradingStrategyResultRecord =
        {
            StrategyName:string
            Ticker:string
            Profit:decimal
            RR:decimal
            ReturnPct:decimal
            NumberOfShares:decimal
            Cost:decimal
            AverageBuyCostPerShare:decimal
            AverageSaleCostPerShare:decimal
            Opened:string
            Closed:string
            DaysHeld:int
        }
        
    let strategyPerformance (writer:ICSVWriter) (strategies:seq<TradingStrategyPerformance>) =
        
        let rows =
            strategies
            |> Seq.collect (fun strategy -> 
                strategy.positions
                |> Seq.map (fun r ->
                    {
                        StrategyName = strategy.strategyName
                        Ticker = r.Ticker.Value
                        Profit = System.Math.Round(r.Profit, 2)
                        RR = System.Math.Round(r.RR, 2)
                        ReturnPct = System.Math.Round(r.GainPct, 2) * 100m
                        NumberOfShares = r.CompletedPositionShares
                        Cost = r.CompletedPositionShares * r.CompletedPositionCostPerShare
                        AverageBuyCostPerShare = r.CompletedPositionCostPerShare
                        AverageSaleCostPerShare = r.AverageSaleCostPerShare
                        Opened = r.Opened.ToString(DATE_FORMAT)
                        Closed = r.Closed.Value.ToString(DATE_FORMAT)
                        DaysHeld = r.DaysHeld
                    }
                )
            )
            
        writer.Generate(rows)
    
    let pendingPositions (writer:ICSVWriter) (pending:seq<PendingStockPosition>) =
        
        let rows =
            pending
            |> Seq.map (fun p ->
                {
                    Ticker = p.State.Ticker.Value
                    Bid = p.State.Bid
                    NumberOfShares = p.State.NumberOfShares
                    StopPrice = (if p.State.StopPrice.HasValue then Some(p.State.StopPrice.Value) else None)
                    Date = p.State.Date.ToString(DATE_FORMAT)
                    Closed = (if p.State.Closed.HasValue then Some (p.State.Closed.Value.ToString(DATE_FORMAT)) else None)
                    Purchased = p.State.Purchased
                    Strategy = p.State.Strategy
                    Notes = p.State.Notes
                }
            )
            
        writer.Generate(rows)
        
    let stockList (writer:ICSVWriter) (list:StockListState) (justTickers:bool) =
        
        match justTickers with
        | true -> writer.Generate(list.Tickers |> Seq.map (fun s -> { Ticker = s.Ticker.Value }))
        | false -> writer.Generate(list.Tickers |> Seq.map (fun s -> { Ticker = s.Ticker.Value; Created = s.When.ToString(DATE_FORMAT); Notes = s.Note }))
        
    
    let trades culture (writer:ICSVWriter) (trades:seq<StockPositionWithCalculations>) =
        
        let rows =
            trades
            |> Seq.map (fun t ->
                {
                    Symbol = t.Ticker.Value
                    PositionType = t.StockPositionType
                    NumberOfShares = (if t.IsClosed then t.CompletedPositionShares else t.NumberOfShares) |> _.ToString(NUMBER_FORMAT, culture)
                    Opened = t.Opened.ToString(DATE_FORMAT, culture)
                    Closed = (if t.Closed.IsSome then t.Closed.Value.ToString(DATE_FORMAT, culture) else "")
                    DaysHeld = t.DaysHeld
                    AverageCostPerShare = t.CompletedPositionCostPerShare.ToString(CURRENCY_FORMAT, culture)
                    LastSellCostPerShare = (if t.ClosePrice.IsSome then t.ClosePrice.Value.ToString(CURRENCY_FORMAT, culture) else "") 
                    Cost = t.Cost.ToString(CURRENCY_FORMAT, culture)
                    Profit = t.Profit.ToString(CURRENCY_FORMAT, culture)
                    ReturnPct = t.GainPct.ToString(PERCENT_FORMAT, culture)
                    RR = t.RR.ToString(NUMBER_FORMAT, culture)
                    RiskedAmount = (if t.RiskedAmount.IsSome then t.RiskedAmount.Value.ToString(CURRENCY_FORMAT, culture) else "")
                    Strategy= match t.TryGetLabelValue("strategy") with | true, v -> v | _ -> ""
                    Grade = if t.Grade.IsSome then t.Grade.Value.Value else ""
                    GradeNote = (if t.GradeNote.IsSome then t.GradeNote.Value else "")
                }
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
                        Date = cp.When.ToString(DATE_FORMAT)
                    }
                | :? CryptoSold as cs ->
                    {
                        Symbol = o.State.Token
                        Type = "sell"
                        Amount = cs.Quantity
                        Price = cs.DollarAmount
                        Date = cs.When.ToString(DATE_FORMAT)
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
                match e.Type with
                | Buy -> 
                    {
                        Ticker = o.Ticker.Value
                        Type = "buy"
                        Amount = e.NumberOfShares
                        Price = e.Price
                        Date = e.Date.ToString(DATE_FORMAT)
                    }
                | Sell ->
                    {
                        Ticker = o.Ticker.Value
                        Type = "sell"
                        Amount = e.NumberOfShares
                        Price = e.Price
                        Date = e.Date.ToString(DATE_FORMAT)
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
                        Expiration = o.State.Expiration.ToString(DATE_FORMAT)
                        Amount = os.NumberOfContracts |> decimal
                        Premium = os.Premium
                        Filled = os.When.ToString(DATE_FORMAT)
                    }
                | :? OptionPurchased as op ->
                    {
                        Ticker = o.State.Ticker.Value
                        Type = "buy"
                        Strike = o.State.StrikePrice
                        OptionType = o.State.OptionType.ToString()
                        Expiration = o.State.Expiration.ToString(DATE_FORMAT)
                        Amount = op.NumberOfContracts |> decimal
                        Premium = op.Premium
                        Filled = op.When.ToString(DATE_FORMAT)
                    }
                | :? OptionExpired as oe ->
                    {
                        Ticker = o.State.Ticker.Value
                        Type = (if oe.Assigned then "assigned" else "expired")
                        Strike = o.State.StrikePrice
                        OptionType = o.State.OptionType.ToString()
                        Expiration = o.State.Expiration.ToString(DATE_FORMAT)
                        Amount = 0m
                        Premium = 0m
                        Filled = oe.When.ToString(DATE_FORMAT)
                    }
                | _ -> { Ticker = ""; Type = ""; Strike = 0m; OptionType = ""; Expiration = ""; Amount = 0m; Premium = 0m; Filled = "" }
            )
            |> Seq.filter (fun r -> r.Ticker <> "")
            
        writer.Generate(rows)


    let generateFilename prefix =
        prefix + "_" + System.DateTime.UtcNow.ToString("yyyyMMdd_hhmss") + ".csv"



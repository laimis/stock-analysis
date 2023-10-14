namespace core.fs.Shared.Adapters.CSV

open core.Cryptos
open core.Notes
open core.Options
open core.Portfolio
open core.Shared
open core.Stocks
open core.Stocks.Services.Trading
open core.fs.Shared.Domain.Accounts

type ExportResponse(filename:string, content:string) = 
    member this.Filename = filename
    member this.Content = content
    member this.ContentType = "text/csv"
        
type ICSVWriter =
    abstract Generate<'T> : rows:seq<'T> -> string
    
type ICSVParser =
    abstract Parse<'T> : content:string -> core.Shared.ServiceResponse<seq<'T>>
    

module CSVExport =
    
    let private DATE_FORMAT = "yyyy-MM-dd"
    
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
            Notes:string
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
            Opened:string
            Closed:string
            DaysHeld:int
            FirstBuyCost:decimal
            Cost:decimal
            Profit:decimal
            ReturnPct:decimal
            RR:decimal
            RiskedAmount:decimal option
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
        
    
    let trades (writer:ICSVWriter) (trades:seq<PositionInstance>) =
        
        let rows =
            trades
            |> Seq.map (fun t ->
                {
                    Symbol = t.Ticker.Value
                    Opened = t.Opened.ToString(DATE_FORMAT)
                    Closed = (if t.Closed.HasValue then t.Closed.Value.ToString(DATE_FORMAT) else "")
                    DaysHeld = t.DaysHeld
                    FirstBuyCost = t.CompletedPositionCostPerShare
                    Cost = t.Cost
                    Profit = t.Profit
                    ReturnPct = t.GainPct
                    RR = t.RR
                    RiskedAmount = (if t.RiskedAmount.HasValue then Some(t.RiskedAmount.Value) else None)
                    Grade = if t.Grade.HasValue then t.Grade.Value.Value else ""
                    GradeNote = t.GradeNote
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
        
    let stocks (writer:ICSVWriter) (stocks:seq<OwnedStock>) =
        
        let rows =
            stocks
            |> Seq.collect (fun o -> o.State.BuyOrSell |> Seq.map (fun e -> (o, e)))
            |> Seq.sortBy (fun (_, e) -> e.When)
            |> Seq.map (fun (o, e) ->
                match e with
                | :? StockPurchased as sp -> 
                    {
                        Ticker = o.State.Ticker.Value
                        Type = "buy"
                        Amount = sp.NumberOfShares
                        Price = sp.Price
                        Date = sp.When.ToString(DATE_FORMAT)
                        Notes = sp.Notes
                    }
                | :? StockSold as ss ->
                    {
                        Ticker = o.State.Ticker.Value
                        Type = "sell"
                        Amount = ss.NumberOfShares
                        Price = ss.Price
                        Date = ss.When.ToString(DATE_FORMAT)
                        Notes = ss.Notes
                    }
                | _ -> { Ticker = ""; Type = ""; Amount = 0m; Price = 0m; Date = ""; Notes = "" }
            )
            |> Seq.filter (fun r -> r.Ticker <> "")
            
        writer.Generate(rows)
        
    let notes (writer:ICSVWriter) (notes:seq<Note>) =
        
        let rows =
            notes
            |> Seq.map (fun n ->
                {
                    Created = n.State.Created.ToString(DATE_FORMAT)
                    Ticker = n.State.RelatedToTicker
                    Note = n.State.Note
                }
            )
            
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

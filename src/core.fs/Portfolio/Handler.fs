namespace core.fs.Portfolio

open System
open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open Microsoft.FSharp.Core
open core.Cryptos
open core.Options
open core.Shared
open core.fs.Services
open core.fs.Services.Trading
open core.fs.Services.TradingStrategies
open core.fs.Shared
open core.fs.Shared.Adapters.Brokerage
open core.fs.Shared.Adapters.CSV
open core.fs.Shared.Adapters.Storage
open core.fs.Shared.Domain
open core.fs.Shared.Domain.Accounts

type Query =
    {
        UserId: UserId
    }
    
type ImportStocks =
    {
        UserId: UserId
        Content: string
    }

type private ImportRecord =
    {
         amount:decimal
         ``type``:string
         date:Nullable<DateTimeOffset>
         price:decimal
         ticker:string
    }

    
type StockTransaction =
    {
        [<Required>]
        PositionId: StockPositionId
        [<Range(1, 1000000)>]
        NumberOfShares: decimal
        [<Range(0, 100000)>]
        Price: decimal
        [<Required>]
        Date: Nullable<DateTimeOffset>
        StopPrice: Nullable<decimal>
        Notes: string option
        BrokerageOrderId: string option
        Ticker: Ticker
    }
    
type BuyOrSell =
    | Buy of StockTransaction * UserId
    | Sell of StockTransaction * UserId

type DeleteTransaction =
    {
        PositionId: StockPositionId
        UserId: UserId
        TransactionId: Guid
    }

type OpenLongStockPosition = {
    [<Range(1, 1000000)>]
    NumberOfShares: decimal
    [<Range(0, 100000)>]
    Price: decimal
    [<Required>]
    Date: DateTimeOffset option
    StopPrice: decimal option
    Notes: string option
    Strategy: string option
    Ticker: Ticker
}
 
type DeletePosition =
    {
        PositionId: StockPositionId
        Ticker: Ticker
        UserId: UserId
    }
    
type DeleteStop =
    {
        [<Required>]
        PositionId: StockPositionId
    }
    
type SetStop =
    {
        [<Required>]
        StopPrice:decimal option
        [<Required>]
        PositionId:StockPositionId
    }

type GradePosition =
    {
        [<Required>]
        PositionId: StockPositionId
        [<Required>]
        Ticker: Ticker
        [<Required>]
        Grade: TradeGrade
        Note: string option
    }
    
type RemoveLabel =
    {
        PositionId: StockPositionId
        [<Required>]
        Ticker: Ticker
        UserId: UserId
        [<Required>]
        Key: string
    }
    
type AddLabel =
    {
        [<Required>]
        PositionId: StockPositionId
        [<Required>]
        Ticker: Ticker
        [<Required>]
        Key: string
        [<Required>]
        Value: string
    }
    
type ProfitPointsQuery =
    {
        NumberOfPoints: int
        [<Required>]
        PositionId: StockPositionId
        UserId: UserId
        [<Required>]
        Ticker: Ticker
    }
    
type SetRisk =
    {
        [<Required>]
        PositionId: StockPositionId
        [<Required>]
        Ticker: Ticker
        [<Required>]
        RiskAmount: decimal option
    }
    
type SimulateTrade = 
    {
        [<Required>]
        Ticker: Ticker
        UserId: UserId
        [<Required>]
        PositionId: StockPositionId
    }
    
type SimulateTradeForTicker =
    {
        Date: DateTimeOffset
        NumberOfShares: decimal
        Price: decimal
        StopPrice: decimal option
        Ticker: Ticker
        UserId: UserId
    }
    
type SimulateUserTrades =
    {
        UserId: UserId
        NumberOfTrades: int
        ClosePositionIfOpenAtTheEnd: bool
    }
    
type ExportUserSimulatedTrades =
    {
        UserId: UserId
        NumberOfTrades: int
        ClosePositionIfOpenAtTheEnd: bool
    }

type QueryTradingEntries =
    {
        UserId: UserId
    }
    
type QueryPastTradingEntries =
    {
        UserId: UserId
    }
    
type QueryTransactions =
    {
        UserId: UserId
        Show:string
        GroupBy:string
        TxType:string
        Ticker:Nullable<Ticker>
    }
    
type TransactionSummary =
    {
        Period: string
        UserId: UserId
    }
    
    member this.GetDates() =
        let start = DateTimeOffset.UtcNow.Date.AddDays(-7)
        let ``end`` = DateTimeOffset.UtcNow.Date.AddDays(1);

        match this.Period with
        | "last7days" ->
            (start, ``end``)
        | _ ->
            let date = DateTimeOffset.UtcNow.Date;
            let offset = int date.DayOfWeek - 1;
            let toSubtract =
                match offset with
                | x when x < 0 -> 6
                | _ -> offset

            let monday = date.AddDays(-1.0 * (toSubtract |> float));
            let sunday = monday.AddDays(7)
            
            (monday, sunday)
    
type Handler(accounts:IAccountStorage,brokerage:IBrokerage,csvWriter:ICSVWriter,storage:IPortfolioStorage,marketHours:IMarketHours,csvParser:ICSVParser) =
    
    interface IApplicationService
    
    member _.Handle(cmd:BuyOrSell) = task {
        
        let buyOrSellFunction =
            match cmd with
            | Buy (data, _) -> StockPosition.buy data.NumberOfShares data.Price data.Date.Value data.Notes
            | Sell (data, _) -> StockPosition.sell data.NumberOfShares data.Price data.Date.Value data.Notes
            
        let data, userId =
            match cmd with
            | Buy (data, userId) -> (data, userId)
            | Sell (data, userId) -> (data, userId)
            
        let! user = accounts.GetUser(userId)
        
        match user with
        | None -> return "User not found" |> ResponseUtils.failed
        | _ ->
            let! stock = storage.GetStockPosition data.PositionId userId
            
            match stock with
            | None -> return "Stock position not found" |> ResponseUtils.failed
            | Some position ->
                
                do!
                    position
                    |> buyOrSellFunction
                    |> storage.SaveStockPosition userId stock
                
                return Ok
    }
    
    member this.Handle (cmd:ImportStocks) = task {
        let records = csvParser.Parse<ImportRecord>(cmd.Content)
        match records.Success with
        | None -> return records |> ResponseUtils.toOkOrError
        | Some records ->
            let! results =
                records
                |> Seq.map (fun r -> async {
                    let command =
                        match r.``type`` with
                        | "buy" -> Buy({
                            PositionId = StockPositionId(Guid.NewGuid())
                            NumberOfShares = r.amount
                            Price = r.price
                            Date = r.date
                            StopPrice = Nullable<decimal>()
                            Notes = None
                            BrokerageOrderId = None
                            Ticker = Ticker(r.ticker)
                        }, cmd.UserId)
                        | "sell" -> Sell({
                            NumberOfShares = r.amount
                            PositionId = StockPositionId(Guid.NewGuid())
                            Price = r.price
                            Date = r.date
                            StopPrice = Nullable<decimal>()
                            Notes = None
                            BrokerageOrderId = None
                            Ticker = Ticker(r.ticker)
                        }, cmd.UserId)
                        | _ -> failwith "Unknown transaction type"
                        
                    let! result = this.Handle(command) |> Async.AwaitTask
                    return result
                })
                |> Async.Sequential
                |> Async.StartAsTask
                
            return results |> ResponseUtils.toOkOrConcatErrors
    }
    
    member _.Handle(userId:UserId, cmd:OpenLongStockPosition) = task {
        let! user = userId |> accounts.GetUser
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<StockPositionWithCalculations>
        | Some _ ->
            let! stocks = storage.GetStockPositions userId
            
            // check if we already have an open position for the ticker
            let openPosition = stocks |> Seq.tryFind (fun x -> x.Ticker = cmd.Ticker && x.Closed = None)
            
            match openPosition with
            | Some _ ->
                return "Position already open" |> ResponseUtils.failedTyped<StockPositionWithCalculations>
            | None ->
                let newPosition =
                    StockPosition.openLong cmd.Ticker cmd.Date.Value
                    |> StockPosition.buy cmd.NumberOfShares cmd.Price cmd.Date.Value cmd.Notes
                    |> StockPosition.setStop cmd.StopPrice cmd.Date.Value
                    |> fun x ->
                        match cmd.Strategy with
                        | Some strategy -> x |> StockPosition.setLabel "strategy" strategy cmd.Date.Value
                        | None -> x
                
                do! newPosition |> storage.SaveStockPosition userId openPosition
                
                // check if we have any pending positions for the ticker
                let! pendingPositions = storage.GetPendingStockPositions userId
                let pendingPositionOption = pendingPositions |> Seq.tryFind (fun x -> x.State.Ticker = cmd.Ticker && x.State.IsClosed = false)
                
                match pendingPositionOption with
                | Some pendingPosition ->
                    
                    // transfer some data from pending position to this new position
                    let positionWithStop = newPosition |> StockPosition.setStop (Some pendingPosition.State.StopPrice.Value) cmd.Date.Value
                    
                    let positionWithNotes = 
                        match positionWithStop.Notes with
                        | [] when String.IsNullOrWhiteSpace(pendingPosition.State.Notes) = false -> positionWithStop |> StockPosition.addNotes (Some pendingPosition.State.Notes) cmd.Date.Value
                        | _ -> positionWithStop
                    
                    let positionWithStrategy =
                        match pendingPosition.State.Strategy with
                        | null -> positionWithNotes
                        | _ -> positionWithNotes |> StockPosition.setLabel "strategy" pendingPosition.State.Strategy cmd.Date.Value
                        
                    do! positionWithStrategy |> storage.SaveStockPosition userId (Some newPosition)
                    
                    let withCalculations =
                        positionWithStrategy
                        |> StockPositionWithCalculations
                    
                    withCalculations.AverageCostPerShare |> pendingPosition.Purchase
                    
                    do! storage.SavePendingPosition pendingPosition userId
                    
                    return withCalculations |> ResponseUtils.success
                | None ->
                    return newPosition |> StockPositionWithCalculations |> ResponseUtils.success
    }

    member this.Handle (command:DeletePosition) = task {
        let! user = accounts.GetUser(command.UserId)
        match user with
        | None -> return "User not found" |> ResponseUtils.failed
        | _ ->
            let! position = storage.GetStockPosition command.PositionId command.UserId
            
            match position with
            | None -> return "Stock position not found" |> ResponseUtils.failed
            | Some position ->
                let deletedPosition = position |> StockPosition.delete 
                do! deletedPosition |> storage.SaveStockPosition command.UserId (Some position)
                return Ok
    }
    
    member _.Handle (userId:UserId, command:DeleteStop) = task {
        let! user = accounts.GetUser(userId)
        match user with
        | None -> return "User not found" |> ResponseUtils.failed
        | _ ->
            let! stock = storage.GetStockPosition command.PositionId userId
            match stock with
            | None -> return "Stock position not found" |> ResponseUtils.failed
            | Some existing ->
                do!
                    existing
                    |> StockPosition.deleteStop DateTimeOffset.UtcNow
                    |> storage.SaveStockPosition userId stock
                return Ok
    }
    
    member _.Handle (cmd:DeleteTransaction) = task {
        let! user = accounts.GetUser(cmd.UserId)
        match user with
        | None -> return "User not found" |> ResponseUtils.failed
        | _ ->
            let! stock = storage.GetStockPosition cmd.PositionId cmd.UserId
            match stock with
            | None -> return "Stock position not found" |> ResponseUtils.failed
            | Some existing ->
                do!
                    existing
                    |> StockPosition.deleteTransaction cmd.TransactionId DateTimeOffset.UtcNow
                    |> storage.SaveStockPosition cmd.UserId stock
                    
                return Ok
    }
        
    member this.Handle (query:Query) = task {
        let! stocks = query.UserId |> storage.GetStockPositions
        
        let openStocks = stocks |> Seq.filter _.IsOpen
        
        let! options = storage.GetOwnedOptions(query.UserId)
        let openOptions =
            options
            |> Seq.filter (fun o -> o.State.Closed.HasValue = false)
            |> Seq.sortBy _.State.Expiration
            |> Seq.toList
            
        let! cryptos = storage.GetCryptos(query.UserId)
        
        let view =
            {
                OpenStockCount = openStocks |> Seq.length
                OpenOptionCount = openOptions.Length
                OpenCryptoCount = cryptos |> Seq.length
            }
            
        return view |> ResponseUtils.success    
    }
    
    member _.HandleGradePosition userId (command:GradePosition) = task {
        let! user = accounts.GetUser userId
        match user with
        | None -> return "User not found" |> ResponseUtils.failed
        | _ ->
            let! stock = storage.GetStockPosition command.PositionId userId
                
            match stock with
            | None -> return "Stock position not found" |> ResponseUtils.failed
            | Some stock ->
                
                do! stock
                    |> StockPosition.assignGrade command.Grade command.Note DateTimeOffset.UtcNow
                    |> storage.SaveStockPosition userId (Some stock)
                
                return Ok
    }
    
    member _.Handle (command:RemoveLabel) = task {
        let! user = accounts.GetUser(command.UserId)
        match user with
        | None -> return "User not found" |> ResponseUtils.failed
        | _ ->
            let! stock = storage.GetStockPosition command.PositionId command.UserId
            
            match stock with
            | None -> return "Stock not found" |> ResponseUtils.failed
            | Some stock ->
                
                do!
                    stock
                    |> StockPosition.deleteLabel command.Key DateTimeOffset.UtcNow
                    |> storage.SaveStockPosition command.UserId (Some stock)
                
                return Ok
    }
    
    member _.HandleAddLabel userId (command:AddLabel) = task {
        let! user = accounts.GetUser userId
        match user with
        | None -> return "User not found" |> ResponseUtils.failed
        | _ ->
            let! stock = storage.GetStockPosition command.PositionId userId
            
            match stock with
            | None -> return "Stock position not found" |> ResponseUtils.failed
            | Some stock ->
                
                do! stock
                    |> StockPosition.setLabel command.Key command.Value DateTimeOffset.UtcNow
                    |> storage.SaveStockPosition userId (Some stock)
                
                return Ok
    }
    
    member _.Handle (query:ProfitPointsQuery) = task {
        let! user = accounts.GetUser(query.UserId)
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<ProfitPoints.ProfitPointContainer []>
        | _ ->
            let! stock = storage.GetStockPosition query.PositionId query.UserId 
            
            match stock with
            | None -> return "Stock position not found" |> ResponseUtils.failedTyped<ProfitPoints.ProfitPointContainer []>
            | Some stock ->
                
                let stock = stock |> StockPositionWithCalculations
                
                let stopBased =
                    stock
                    |> ProfitPoints.getProfitPointsWithStopPrice query.NumberOfPoints
                
                let percentBased level =
                    stock
                    |> ProfitPoints.getProfitPointWithPercentGain level TradingStrategyConstants.AvgPercentGain
                    
                let percentBased = ProfitPoints.getProfitPoints percentBased query.NumberOfPoints
                    
                let arr =
                    [|
                        ProfitPoints.ProfitPointContainer("Stop based", prices=stopBased)
                        ProfitPoints.ProfitPointContainer($"{TradingStrategyConstants.AvgPercentGain}%% intervals", prices=percentBased)
                    |]
                
                return ServiceResponse<ProfitPoints.ProfitPointContainer []>(arr)
    }
    
    member _.HandleSetRisk userId (command:SetRisk) = task {
        let! user = accounts.GetUser userId
        match user with
        | None -> return "User not found" |> ResponseUtils.failed
        | _ ->
            let! stock = storage.GetStockPosition command.PositionId userId
            match stock with
            | None -> return "Stock not found" |> ResponseUtils.failed
            | Some stock ->
                
                do!
                    stock
                    |> StockPosition.setRiskAmount command.RiskAmount.Value DateTimeOffset.UtcNow
                    |> storage.SaveStockPosition userId (Some stock)
                
                return Ok
    }
    
    member _.Handle (command:SimulateTrade) = task {
        let! user = accounts.GetUser(command.UserId)
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<TradingStrategyResults>
        | Some user ->
            let! stock = storage.GetStockPosition command.PositionId command.UserId
            match stock with
            | None -> return "Stock position not found" |> ResponseUtils.failedTyped<TradingStrategyResults>
            | Some stock ->
                let runner = TradingStrategyRunner(brokerage, marketHours)
                let! simulation = runner.Run(user.State, position=stock, closeIfOpenAtTheEnd=false)
                return ServiceResponse<TradingStrategyResults>(simulation)
    }
    
    member _.Handle (command:SimulateTradeForTicker) = task {
        let! user = accounts.GetUser(command.UserId)
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<TradingStrategyResults>
        | Some user ->
            let runner = TradingStrategyRunner(brokerage, marketHours)
                
            let! results = runner.Run(
                    user.State,
                    numberOfShares=command.NumberOfShares,
                    price=command.Price,
                    stopPrice=command.StopPrice,
                    ticker=command.Ticker,
                    ``when``= command.Date,
                    closeIfOpenAtTheEnd=false
                )
                
            return ServiceResponse<TradingStrategyResults>(results)
    }
    
    member _.Handle (command:SimulateUserTrades) = task {
        
        let runSimulation (runner:TradingStrategyRunner) user (position:StockPositionState) closeIfOpenAtEnd = async {
            let calculations = position |> StockPositionWithCalculations
            let! results =
                runner.Run(
                    user,
                    numberOfShares=calculations.CompletedPositionShares,
                    price=calculations.CompletedPositionCostPerShare,
                    stopPrice=calculations.FirstStop,
                    ticker=position.Ticker,
                    ``when``=position.Opened,
                    closeIfOpenAtTheEnd=closeIfOpenAtEnd
                ) |> Async.AwaitTask
            
            let actualTradingResult = {
                StrategyName = TradingStrategyConstants.ActualTradesName
                Position = position
                MaxDrawdownPct = 0m
                MaxGainPct = 0m
                MaxDrawdownPctRecent = 0m
                MaxGainPctRecent = 0m 
            }
            results.Insert(0, actualTradingResult)
            return results.Results
        }
        
        let mapToStrategyPerformance (name:string, results:TradingStrategyResult seq) =
            let positions = results |> Seq.map (fun r -> r.Position |> StockPositionWithCalculations) |> Seq.toArray
            let performance =
                try
                    TradingPerformance.Create(positions)
                with
                    // TODO: something is throwing Value was either too large or too small for a Decimal
                    // for certain simulations.
                    // ignoring it here because I need the results, but need to look at it at some point
                    | :?OverflowException -> TradingPerformance.Create(Array.Empty<StockPositionWithCalculations>())
            
            {performance = performance; strategyName = name; positions = positions}
        
        let! user = accounts.GetUser(command.UserId)
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<TradingStrategyPerformance array>
        | Some user ->
            
            let! stocks = storage.GetStockPositions command.UserId
            
            let positions =
                stocks
                |> Seq.filter _.StopPrice.IsSome
                |> Seq.sortByDescending (fun p -> match p.Closed with | Some c -> c | None -> DateTimeOffset.MinValue)
                |> Seq.take command.NumberOfTrades
                |> Seq.toList
                
            let runner = TradingStrategyRunner(brokerage, marketHours)
            
            let! simulations =
                positions
                |> Seq.map (fun p -> runSimulation runner user.State p command.ClosePositionIfOpenAtTheEnd)
                |> Async.Sequential
                |> Async.StartAsTask
                
            let results =
                simulations
                |> Seq.concat
                |> Seq.groupBy (_.StrategyName)
                |> Seq.map mapToStrategyPerformance
                |> Seq.toArray
                
            return ServiceResponse<TradingStrategyPerformance array>(results)
    }
    
    member this.HandleExport (command:ExportUserSimulatedTrades) = task {
        let (simulateCommand:SimulateUserTrades) = {
            UserId = command.UserId
            NumberOfTrades = command.NumberOfTrades
            ClosePositionIfOpenAtTheEnd = command.ClosePositionIfOpenAtTheEnd}
        let! results = this.Handle(simulateCommand)
        match results.IsOk with
        | true ->
            let content = CSVExport.strategyPerformance csvWriter results.Success.Value
            let filename = CSVExport.generateFilename $"simulated-trades-{command.NumberOfTrades}"
            let response = ExportResponse(filename, content);
            return ServiceResponse<ExportResponse>(response);
            
        | false -> return results.Error.Value.Message |> ResponseUtils.failedTyped<ExportResponse>
    }
    
    member _.Handle (query:QueryTradingEntries) = task {
        let! user = accounts.GetUser(query.UserId)
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<TradingEntriesView>
        | Some user ->
            let! stocks = storage.GetStockPositions query.UserId
            
            let positions =
                stocks
                |> Seq.filter _.IsOpen
                |> Seq.map (fun s -> s |> StockPositionWithCalculations)
                |> Seq.toArray
                
            let! accountResponse = brokerage.GetAccount(user.State)
            let account =
                match accountResponse.Success with
                | Some acc -> acc
                | None -> TradingAccount.Empty
                
            let tickers =
                positions
                |> Seq.map (_.Ticker)
                |> Seq.append (account.StockPositions |> Seq.map (_.Ticker))
                |> Seq.distinct
                
            let! pricesResponse = brokerage.GetQuotes user.State tickers
            let prices =
                match pricesResponse.Success with
                | Some prices -> prices
                | None -> Dictionary<Ticker, StockQuote>()
                
            let pricesWithStringAsKey = prices.Keys |> Seq.map (fun p -> p.Value, prices[p]) |> Map.ofSeq
            
            let current = positions |> Array.sortByDescending _.RR
            
            let violations = core.fs.Helpers.getViolations account.StockPositions positions prices |> Seq.toArray;
            
            let (tradingEntries:TradingEntriesView) =
                {
                    current=current
                    violations=violations
                    cashBalance=account.CashBalance
                    brokerageOrders=account.Orders
                    prices=pricesWithStringAsKey
                }
            return ServiceResponse<TradingEntriesView>(tradingEntries)
    }
    
    member _.Handle (query:QueryPastTradingEntries) = task {
        let! user = accounts.GetUser(query.UserId)
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<PastTradingEntriesView>
        | _ ->
            let! stocks = storage.GetStockPositions query.UserId
            
            let past =
                stocks
                |> Seq.filter _.IsClosed
                |> Seq.sortByDescending _.Closed.Value
                |> Seq.map StockPositionWithCalculations
                |> Seq.toArray
            
            let performance = TradingPerformanceContainerView(past)
            
            let strategyByPerformance =
                past
                |> Seq.filter _.ContainsLabel(key="strategy")
                |> Seq.groupBy _.GetLabelValue(key="strategy")
                |> Seq.map (fun (name, positions) ->
                    let performance = TradingPerformance.Create(positions)
                    {strategyName = name; performance = performance; positions = (positions |> Seq.toArray)}
                )
                |> Seq.sortByDescending _.performance.Profit
                |> Seq.toArray
            let (tradingEntries:PastTradingEntriesView) =
                {
                    past=past;
                    performance=performance;
                    strategyPerformance=strategyByPerformance;
                }
                
            return ServiceResponse<PastTradingEntriesView>(tradingEntries)
    }
    
    member _.Handle(query:QueryTransactions) = task {
        
        let toTransactionsView (stocks:StockPositionWithCalculations seq) (options:OwnedOption seq) (cryptos:OwnedCrypto seq) =
            let tickers = stocks |> Seq.map (_.Ticker) |> Seq.append (options |> Seq.map (_.State.Ticker)) |> Seq.distinct |> Seq.sort |> Seq.toArray
            
            // let stockTransactions =
            //     match query.Show = "shares" || query.Show = null with
            //     | true ->
            //         stocks
            //         |> Seq.filter (fun s -> query.Ticker.HasValue = false || s.Ticker = query.Ticker.Value)
            //         |> Seq.collect toPlTransactions
            //     | false -> Seq.empty
                
            let optionTransactions =
                match query.Show = "options" || query.Show = null with
                | true ->
                    options
                    |> Seq.filter (fun o -> query.Ticker.HasValue = false || o.State.Ticker = query.Ticker.Value)
                    |> Seq.collect (fun o -> o.State.Transactions)
                    |> Seq.filter (fun t -> if query.TxType = "pl" then t.IsPL else t.IsPL |> not)
                | false -> Seq.empty
                
            let cryptoTransactions =
                match query.Show = "cryptos" || query.Show = null with
                | true ->
                    cryptos
                    |> Seq.filter (fun c -> query.Ticker.HasValue = false || c.State.Token = query.Ticker.Value.Value)
                    |> Seq.collect (fun c -> c.State.Transactions)
                    |> Seq.map (fun c -> c.ToSharedTransaction())
                    |> Seq.filter (fun t -> if query.TxType = "pl" then t.IsPL else t.IsPL |> not)
                | false -> Seq.empty
                
            // TODO: reimplement stock transations
            let log = Seq.empty |> Seq.append optionTransactions |> Seq.append cryptoTransactions
                
            TransactionsView(log, query.GroupBy, tickers);
            
        let! stocks = storage.GetStockPositions(query.UserId)
        let! options = storage.GetOwnedOptions(query.UserId)
        let! cryptos = storage.GetCryptos(query.UserId)
        
        let transactionsView = toTransactionsView (stocks |> Seq.map StockPositionWithCalculations) options cryptos
        
        return ServiceResponse<TransactionsView>(transactionsView)
    }
    
    member _.Handle (query:TransactionSummary) = task {
        
        let! stocks = storage.GetStockPositions query.UserId
        let! options = storage.GetOwnedOptions(query.UserId)
        let start, ``end`` = query.GetDates()
        
        let stocks = stocks |> Seq.map StockPositionWithCalculations |> Seq.toArray
             
        let optionTransactions =
            options
            |> Seq.collect (fun o -> o.State.Transactions)
            |> Seq.filter (fun t -> t.DateAsDate >= DateTimeOffset(start))
            |> Seq.filter (fun t -> t.IsPL |> not)
            |> Seq.sortBy (fun t -> t.Ticker)
            |> Seq.toList
            
        let plStockTransactions =
            stocks
            |> Seq.collect _.PLTransactions
            |> Seq.sortBy _.Ticker
            |> Seq.toList
            
        let plOptionTransactions =
            options
            |> Seq.collect (fun o -> o.State.Transactions)
            |> Seq.filter (fun t -> t.DateAsDate >= DateTimeOffset(start))
            |> Seq.filter (fun t -> t.IsPL)
            |> Seq.sortBy (fun t -> t.Ticker)
            |> Seq.toList
            
        let closedPositions =
            stocks
            |> Seq.filter (fun p -> p.IsClosed && p.Closed.Value >= DateTimeOffset(start) && p.Closed.Value <= DateTimeOffset(``end``))
            |> Seq.toList
            
        let openPositions =
            stocks
            |> Seq.filter (fun p -> p.IsClosed = false && p.Opened >= DateTimeOffset(start) && p.Opened <= DateTimeOffset(``end``))
            |> Seq.toList
            
        let view = TransactionSummaryView(
            start=start,
            ``end``=``end``,
            openPositions=openPositions,
            closedPositions=closedPositions,
            stockTransactions=Seq.empty,
            optionTransactions=optionTransactions,
            plStockTransactions=plStockTransactions,
            plOptionTransactions=plOptionTransactions
        )
        
        return ServiceResponse<TransactionSummaryView>(view)
    }
    
    member _.HandleStop(userId,cmd:SetStop) = task {
        let! stock = storage.GetStockPosition cmd.PositionId userId
        match stock with
        | None -> return "Stock position not found" |> ResponseUtils.failed
        | Some existing ->
            do!
                existing
                |> StockPosition.setStop cmd.StopPrice DateTimeOffset.UtcNow
                |> storage.SaveStockPosition userId stock
                
            return Ok
    }
namespace core.fs.Portfolio

open System
open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open Microsoft.FSharp.Core
open core.Cryptos
open core.Options
open core.Shared
open core.Shared.Adapters.Brokerage
open core.Shared.Adapters.CSV
open core.Shared.Adapters.Storage
open core.Stocks
open core.Stocks.Services.Trading
open core.fs

type Query =
    {
        UserId: Guid
    }
    
type DeletePosition =
    {
        PositionId: int
        Ticker: Ticker
        UserId: Guid
    }

type GradePosition =
    {
        [<Required>]
        PositionId: int
        [<Required>]
        Ticker: Ticker
        UserId: Guid
        [<Required>]
        Grade: TradeGrade
        Note: string
    }
    
    static member WithUserId (userId:Guid) (command:GradePosition) = { command with UserId = userId }
    
type RemoveLabel =
    {
        PositionId: int
        [<Required>]
        Ticker: Ticker
        UserId: Guid
        [<Required>]
        Key: string
    }
    
type AddLabel =
    {
        [<Required>]
        PositionId: int
        [<Required>]
        Ticker: Ticker
        UserId: Guid
        [<Required>]
        Key: string
        [<Required>]
        Value: string
    }
    
    static member WithUserId (userId:Guid) (command:AddLabel) = { command with UserId = userId }
    
type ProfitPointsQuery =
    {
        [<Required>]
        PositionId: int
        [<Required>]
        Ticker: Ticker
        UserId: Guid
    }
    
type SetRisk =
    {
        [<Required>]
        PositionId: int
        [<Required>]
        Ticker: Ticker
        UserId: Guid
        [<Required>]
        RiskAmount: Nullable<decimal>
    }
    static member WithUserId (userId:Guid) (command:SetRisk) = { command with UserId = userId }
    
type SimulateTrade = 
    {
        [<Required>]
        Ticker: Ticker
        UserId: Guid
        [<Required>]
        PositionId: int
    }
    
type SimulateTradeForTicker =
    {
        Date: DateTimeOffset
        NumberOfShares: decimal
        Price: decimal
        StopPrice: decimal
        Ticker: Ticker
        UserId: Guid
    }
    
type SimulateUserTrades =
    {
        UserId: Guid
        NumberOfTrades: int
        ClosePositionIfOpenAtTheEnd: bool
    }
    
type ExportUserSimulatedTrades =
    {
        UserId: Guid
        NumberOfTrades: int
        ClosePositionIfOpenAtTheEnd: bool
    }

type QueryTradingEntries =
    {
        UserId: Guid
    }
    
type QueryTransactions =
    {
        UserId: Guid
        Show:string
        GroupBy:string
        TxType:string
        Ticker:Nullable<Ticker>
    }
    
type TransactionSummary =
    {
        Period: string
        UserId: Guid
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
    
type Handler(accounts:IAccountStorage,brokerage:IBrokerage,csvWriter:ICSVWriter,storage:IPortfolioStorage,marketHours:IMarketHours) =
    
    interface IApplicationService

    member this.Handle (command:DeletePosition) = task {
        let! account = accounts.GetUser(command.UserId)
        match account with
        | null -> return "User not found" |> ResponseUtils.failed
        | _ ->
            let! stocks = storage.GetStocks(command.UserId)
            
            let stock =
                stocks
                |> Seq.filter (fun s -> s.State.Ticker = command.Ticker)
                |> Seq.tryHead
                
            match stock with
            | None -> return "Stock not found" |> ResponseUtils.failed
            | Some stock ->
                stock.DeletePosition(command.PositionId) |> ignore
                do! storage.Save(stock, command.UserId)
                return ServiceResponse()
    }
        
    member this.Handle (query:Query) = task {
        let! stocks = storage.GetStocks(query.UserId)
        
        let openStocks = stocks |> Seq.filter (fun s -> s.State.OpenPosition = null |> not) |> Seq.map (fun s -> s.State) |> Seq.toList
        
        let! options = storage.GetOwnedOptions(query.UserId)
        let openOptions =
            options
            |> Seq.filter (fun o -> o.State.Closed.HasValue = false)
            |> Seq.sortBy (fun o -> o.State.Expiration)
            |> Seq.toList
            
        let! cryptos = storage.GetCryptos(query.UserId)
        
        let view =
            {
                OpenStockCount = openStocks.Length
                OpenOptionCount = openOptions.Length
                OpenCryptoCount = cryptos |> Seq.length
            }
            
        return view |> ResponseUtils.success    
    }
    
    member _.Handle (command:GradePosition) = task {
        let! account = accounts.GetUser(command.UserId)
        match account with
        | null -> return "User not found" |> ResponseUtils.failed
        | _ ->
            let! stocks = storage.GetStocks(command.UserId)
            
            let stock =
                stocks
                |> Seq.filter (fun s -> s.State.Ticker = command.Ticker)
                |> Seq.tryHead
                
            match stock with
            | None -> return "Stock not found" |> ResponseUtils.failed
            | Some stock ->
                
                match stock.AssignGrade(positionId=command.PositionId, grade=command.Grade, note=command.Note) with
                | false -> ()
                | true -> do! storage.Save(stock, command.UserId)
                
                return ServiceResponse()
    }
    
    member _.Handle (command:RemoveLabel) = task {
        let! account = accounts.GetUser(command.UserId)
        match account with
        | null -> return "User not found" |> ResponseUtils.failed
        | _ ->
            let! stocks = storage.GetStocks(command.UserId)
            
            let stock =
                stocks
                |> Seq.filter (fun s -> s.State.Ticker = command.Ticker)
                |> Seq.tryHead
                
            match stock with
            | None -> return "Stock not found" |> ResponseUtils.failed
            | Some stock ->
                
                match stock.DeletePositionLabel(positionId=command.PositionId, key=command.Key) with
                | false -> ()
                | true -> do! storage.Save(stock, command.UserId)
                
                return ServiceResponse()
    }
    
    member _.Handle (command:AddLabel) = task {
        let! account = accounts.GetUser(command.UserId)
        match account with
        | null -> return "User not found" |> ResponseUtils.failed
        | _ ->
            let! stocks = storage.GetStocks(command.UserId)
            
            let stock =
                stocks
                |> Seq.filter (fun s -> s.State.Ticker = command.Ticker)
                |> Seq.tryHead
                
            match stock with
            | None -> return "Stock not found" |> ResponseUtils.failed
            | Some stock ->
                
                match stock.SetPositionLabel(positionId=command.PositionId, key=command.Key, value=command.Value) with
                | false -> ()
                | true -> do! storage.Save(stock, command.UserId)
                
                return ServiceResponse()
    }
    
    member _.Handle (query:ProfitPointsQuery) = task {
        let! account = accounts.GetUser(query.UserId)
        match account with
        | null -> return "User not found" |> ResponseUtils.failedTyped<ProfitPoints.ProfitPointContainer []>
        | _ ->
            let! stocks = storage.GetStocks(query.UserId)
            
            let stock =
                stocks
                |> Seq.filter (fun s -> s.State.Ticker = query.Ticker)
                |> Seq.tryHead
                
            match stock with
            | None -> return "Stock not found" |> ResponseUtils.failedTyped<ProfitPoints.ProfitPointContainer []>
            | Some stock ->
                Console.WriteLine($"Stock: {stock.State.Ticker}")
                let position = stock.State.GetPosition(query.PositionId)
                match position with
                | null -> return "Position not found" |> ResponseUtils.failedTyped<ProfitPoints.ProfitPointContainer []>
                | _ ->
                    let levels = 4
                    let stopBased =
                        [|1..levels|]
                        |> Array.map (fun i -> ProfitPoints.GetProfitPointWithStopPrice(position, i))
                        |> Array.filter (fun v -> v.HasValue)
                        |> Array.map (fun v -> v.Value)
                    
                    let percentBased =
                        [|1..levels|]
                        |> Array.map (fun i -> ProfitPoints.GetProfitPointWithPercentGain(position, i, TradingStrategyConstants.AvgPercentGain))
                        |> Array.filter (fun v -> v.HasValue)
                        |> Array.map (fun v -> v.Value)
                    
                    let arr = [|
                        ProfitPoints.ProfitPointContainer("Stop based", prices=stopBased)
                        ProfitPoints.ProfitPointContainer($"{TradingStrategyConstants.AvgPercentGain}%% intervals", prices=percentBased)
                        |]
                    
                    return ServiceResponse<ProfitPoints.ProfitPointContainer []>(arr)
    }
    
    member _.Handle (command:SetRisk) = task {
        let! account = accounts.GetUser(command.UserId)
        match account with
        | null -> return "User not found" |> ResponseUtils.failed
        | _ ->
            let! stocks = storage.GetStocks(command.UserId)
            
            let stock =
                stocks
                |> Seq.filter (fun s -> s.State.Ticker = command.Ticker)
                |> Seq.tryHead
                
            match stock with
            | None -> return "Stock not found" |> ResponseUtils.failed
            | Some stock ->
                
                stock.SetRiskAmount(positionId=command.PositionId, riskAmount=command.RiskAmount.Value)
                
                do! storage.Save(stock, command.UserId)
                
                return ServiceResponse()
    }
    
    member _.Handle (command:SimulateTrade) = task {
        let! account = accounts.GetUser(command.UserId)
        match account with
        | null -> return "User not found" |> ResponseUtils.failedTyped<TradingStrategyResults>
        | _ ->
            let! stocks = storage.GetStocks(command.UserId)
            
            let stock =
                stocks
                |> Seq.filter (fun s -> s.State.Ticker = command.Ticker)
                |> Seq.tryHead
                
            match stock with
            | None -> return "Stock not found" |> ResponseUtils.failedTyped<TradingStrategyResults>
            | Some stock ->
                
                let position = stock.State.GetPosition(command.PositionId)
                match position with
                | null -> return "Position not found" |> ResponseUtils.failedTyped<TradingStrategyResults>
                | _ ->
                    let runner = TradingStrategyRunner(brokerage, marketHours)
                    let! simulation = runner.Run(account.State, position=position)
                    return ServiceResponse<TradingStrategyResults>(simulation)
    }
    
    member _.Handle (command:SimulateTradeForTicker) = task {
        let! account = accounts.GetUser(command.UserId)
        match account with
        | null -> return "User not found" |> ResponseUtils.failedTyped<TradingStrategyResults>
        | _ ->
            let runner = TradingStrategyRunner(brokerage, marketHours)
                
            let! results = runner.Run(
                    account.State,
                    numberOfShares=command.NumberOfShares,
                    price=command.Price,
                    stopPrice=command.StopPrice,
                    ticker=command.Ticker,
                    ``when``= command.Date
                )
                
            return ServiceResponse<TradingStrategyResults>(results)
    }
    
    member _.Handle (command:SimulateUserTrades) = task {
        
        let runSimulation (runner:TradingStrategyRunner) user (position:PositionInstance) closeIfOpenAtEnd = async {
            let! results =
                runner.Run(
                    user,
                    numberOfShares=position.CompletedPositionShares,
                    price=position.CompletedPositionCostPerShare,
                    stopPrice=position.FirstStop.Value,
                    ticker=position.Ticker,
                    ``when``=position.Opened.Value,
                    closeIfOpenAtTheEnd=closeIfOpenAtEnd
                ) |> Async.AwaitTask
            
            let actualTradingResult = TradingStrategyResult(0, 0, 0, 0, position, TradingStrategyConstants.ActualTradesName)
            results.Results.Insert(0, actualTradingResult)
            return results.Results
        }
        
        let mapToStrategyPerformance (name:string, results:TradingStrategyResult seq) =
            let positions = results |> Seq.map (fun r -> r.position) |> Seq.toArray
            let performance =
                try
                    TradingPerformance.Create(positions)
                with
                    // TODO: something is throwing Value was either too large or too small for a Decimal
                    // for certain simulations.
                    // ignoring it here because I need the results, but need to look at it at some point
                    | :?OverflowException -> TradingPerformance.Create(Array.Empty<PositionInstance>())
            TradingStrategyPerformance(name, performance, positions)
        
        let! account = accounts.GetUser(command.UserId)
        match account with
        | null -> return "User not found" |> ResponseUtils.failedTyped<TradingStrategyPerformance array>
        | _ ->
            
            let! stocks = storage.GetStocks(command.UserId)
            
            let positions =
                stocks
                |> Seq.map (fun s -> s.State)
                |> Seq.collect (fun s -> s.GetClosedPositions())
                |> Seq.filter (fun p -> p.StopPrice.HasValue)
                |> Seq.sortByDescending (fun p -> p.Closed.Value)
                |> Seq.take command.NumberOfTrades
                |> Seq.toList
                
            let runner = TradingStrategyRunner(brokerage, marketHours)
            
            let! simulations =
                positions
                |> Seq.map (fun p -> runSimulation runner account.State p command.ClosePositionIfOpenAtTheEnd)
                |> Async.Sequential
                |> Async.StartAsTask
                
            let results =
                simulations
                |> Seq.concat
                |> Seq.groupBy (fun r -> r.strategyName)
                |> Seq.map (fun (name, results) -> mapToStrategyPerformance(name, results))
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
            let content = CSVExport.Generate(csvWriter, results.Success);
            let filename = CSVExport.GenerateFilename($"simulated-trades-{command.NumberOfTrades}");
            let response = ExportResponse(filename, content);
            return ServiceResponse<ExportResponse>(response);
            
        | false -> return results.Error.Message |> ResponseUtils.failedTyped<ExportResponse>
    }
    
    member _.Handle (query:QueryTradingEntries) = task {
        let! user = accounts.GetUser(query.UserId)
        match user with
        | null -> return "User not found" |> ResponseUtils.failedTyped<TradingEntriesView>
        | _ ->
            let! stocks = storage.GetStocks(query.UserId)
            
            let positions =
                stocks
                |> Seq.map (fun s -> s.State)
                |> Seq.filter (fun s -> s.OpenPosition = null |> not)
                |> Seq.map (fun s -> s.OpenPosition)
                |> Seq.toArray
                
            let! accountResponse = brokerage.GetAccount(user.State)
            let account =
                match accountResponse.IsOk with
                | true -> accountResponse.Success
                | false -> TradingAccount.Empty
                
            let tickers =
                positions |> Seq.map (fun p -> p.Ticker.Value)
                |> Seq.append (account.StockPositions |> Seq.map (fun p -> p.Ticker.Value))
                |> Seq.distinct
                
            let! pricesResponse = brokerage.GetQuotes(user.State, tickers)
            let prices =
                match pricesResponse.IsOk with
                | true -> pricesResponse.Success
                | false -> Dictionary<string, StockQuote>()
                
            positions |> Array.iter (fun p ->
                match prices.TryGetValue(p.Ticker) with
                | true, price -> p.SetPrice(price.Price)
                | _ -> ()
            )
            
            let current = positions |> Array.sortByDescending (fun p -> p.RR)
            
            let past =
                stocks
                |> Seq.map (fun s -> s.State)
                |> Seq.collect (fun s -> s.GetClosedPositions())
                |> Seq.sortByDescending (fun p -> p.Closed.Value)
                |> Seq.toArray
                
            let performance = TradingPerformanceContainerView(
                past,
                20
            )
            
            let strategyByPerformance =
                past
                |> Seq.filter (fun p -> p.ContainsLabel(key="strategy"))
                |> Seq.groupBy (fun p -> p.GetLabelValue(key="strategy"))
                |> Seq.map (fun (name, positions) ->
                    let performance = TradingPerformance.Create(positions)
                    TradingStrategyPerformance(name, performance, positions |> Seq.toArray)
                )
                |> Seq.sortByDescending (fun p -> p.performance.Profit)
                |> Seq.toArray
                
            let violations = Helpers.getViolations account.StockPositions positions prices |> Seq.toArray;
            
            let (tradingEntries:TradingEntriesView) =
                {
                    current=current;
                    past=past;
                    performance=performance;
                    violations=violations;
                    strategyPerformance=strategyByPerformance;
                    cashBalance=account.CashBalance;
                    brokerageOrders=account.Orders
                }
            return ServiceResponse<TradingEntriesView>(tradingEntries)
    }
    
    member _.Handle(query:QueryTransactions) = task {
        
        let toTransactionsView (stocks:OwnedStock seq) (options:OwnedOption seq) (cryptos:OwnedCrypto seq) =
            let tickers = stocks |> Seq.map (fun s -> s.State.Ticker.Value) |> Seq.append (options |> Seq.map (fun o -> o.State.Ticker)) |> Seq.distinct |> Seq.sort |> Seq.toArray
            
            let stockTransactions =
                match query.Show = "shares" || query.Show = null with
                | true ->
                    stocks
                    |> Seq.filter (fun s -> query.Ticker.HasValue = false || s.State.Ticker = query.Ticker.Value)
                    |> Seq.collect (fun s -> s.State.Transactions)
                    |> Seq.filter (fun t -> if query.TxType = "pl" then t.IsPL else t.IsPL |> not)
                | false -> Seq.empty
                
            let optionTransactions =
                match query.Show = "options" || query.Show = null with
                | true ->
                    options
                    |> Seq.filter (fun o -> query.Ticker.HasValue = false || o.State.Ticker = query.Ticker.Value.Value)
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
                
            let log = stockTransactions |> Seq.append optionTransactions |> Seq.append cryptoTransactions
                
            TransactionsView(log, query.GroupBy, tickers);
            
        let! stocks = storage.GetStocks(query.UserId)
        let! options = storage.GetOwnedOptions(query.UserId)
        let! cryptos = storage.GetCryptos(query.UserId)
        
        let transactionsView = toTransactionsView stocks options cryptos
        
        return ServiceResponse<TransactionsView>(transactionsView)
    }
    
    member _.Handle (query:TransactionSummary) = task {
        
        let! stocks = storage.GetStocks(query.UserId)
        let! options = storage.GetOwnedOptions(query.UserId)
        let start, ``end`` = query.GetDates()
        
        let transactions =
            stocks
            |> Seq.collect (fun s -> s.State.Transactions)
            |> Seq.filter (fun t -> t.DateAsDate >= DateTimeOffset(start))
            |> Seq.append(
                options
                |> Seq.collect (fun o -> o.State.Transactions)
                |> Seq.filter (fun t -> t.DateAsDate >= DateTimeOffset(start))
            )
        
        let stockTransactions =
            transactions
            |> Seq.filter (fun t -> t.IsOption = false && t.IsPL = false)    
            |> Seq.sortBy (fun t -> t.Ticker)
            |> Seq.toList
             
        let optionTransactions =
            transactions
            |> Seq.filter (fun t -> t.IsOption && t.IsPL = false)
            |> Seq.sortBy (fun t -> t.Ticker)
            |> Seq.toList
            
        let plStockTransactions =
            transactions
            |> Seq.filter (fun t -> t.IsOption = false && t.IsPL)
            |> Seq.sortBy (fun t -> t.Ticker)
            |> Seq.toList
            
            
        let plOptionTransactions =
            transactions
            |> Seq.filter (fun t -> t.IsOption && t.IsPL)
            |> Seq.sortBy (fun t -> t.Ticker)
            |> Seq.toList
            
        let closedPositions =
            stocks
            |> Seq.collect (fun s -> s.State.GetClosedPositions())
            |> Seq.filter (fun p -> p.Closed.Value >= DateTimeOffset(start) && p.Closed.Value <= DateTimeOffset(``end``))
            |> Seq.toList
            
        let openPositions =
            stocks
            |> Seq.map (fun s -> s.State.OpenPosition)
            |> Seq.filter (fun p -> p = null |> not)
            |> Seq.filter (fun p -> p.Opened.Value >= DateTimeOffset(start) && p.Opened.Value <= DateTimeOffset(``end``))
            |> Seq.toList
            
        let view = TransactionSummaryView(
            start=start,
            ``end``=``end``,
            openPositions=openPositions,
            closedPositions=closedPositions,
            stockTransactions=stockTransactions,
            optionTransactions=optionTransactions,
            plStockTransactions=plStockTransactions,
            plOptionTransactions=plOptionTransactions
        )
        
        return ServiceResponse<TransactionSummaryView>(view)
    }
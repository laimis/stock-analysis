namespace storage.postgres

open System
open System.Collections.Generic
open System.Data
open System.Linq
open System.Threading.Tasks
open core.fs.Accounts
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Storage
open core.fs.Alerts
open core.fs.Options
open core.Shared
open Dapper
open Microsoft.FSharp.Core
open Npgsql
open storage.shared

// CLIMutable DTOs for Dapper queries
[<CLIMutable>]
type BrokerageOrderDto = 
    {
        orderid: string
        price: decimal
        quantity: decimal
        status: string
        ticker: string
        ordertype: string
        instruction: string
        assettype: string
        executiontime: string
        enteredtime: string
        expirationtime: string
        canbecancelled: bool
    }

[<CLIMutable>]
type BrokerageTransactionDto = 
    {
        transactionid: string
        description: string
        brokeragetype: string
        tradedate: string
        settlementdate: string
        netamount: decimal
        inferredticker: string
        inferredtype: string
        inserted: string
        applied: string
    }

[<CLIMutable>]
type StockPriceAlertDto = 
    {
        alertid: Guid
        userid: Guid
        ticker: string
        pricelevel: decimal
        alerttype: string
        note: string
        state: string
        createdat: DateTimeOffset
        triggeredat: Nullable<DateTimeOffset>
        lastresetat: Nullable<DateTimeOffset>
    }

[<CLIMutable>]
type ReminderDto = 
    {
        reminderid: Guid
        userid: Guid
        date: DateTimeOffset
        message: string
        ticker: string
        state: string
        createdat: DateTimeOffset
        sentat: Nullable<DateTimeOffset>
    }

type AccountStorage(outbox: IOutbox, connectionString: string) =
    inherit PostgresAggregateStorage(outbox, connectionString)
    
    let userEntity = "users"
    
    // Helper functions for discriminated union conversions
    member private _.GetOrderStatusString(status: OrderStatus) : string =
        match status with
        | OrderStatus.Filled -> "Filled"
        | OrderStatus.Working -> "Working"
        | OrderStatus.PendingActivation -> "PendingActivation"
        | OrderStatus.Expired -> "Expired"
        | OrderStatus.Canceled -> "Canceled"
        | OrderStatus.Rejected -> "Rejected"
        | OrderStatus.Accepted -> "Accepted"
        | OrderStatus.Replaced -> "Replaced"
    
    member private _.GetOrderStatusFromString(status: string) : OrderStatus =
        match status with
        | "Filled" -> OrderStatus.Filled
        | "Working" -> OrderStatus.Working
        | "PendingActivation" -> OrderStatus.PendingActivation
        | "Expired" -> OrderStatus.Expired
        | "Canceled" -> OrderStatus.Canceled
        | "Rejected" -> OrderStatus.Rejected
        | "Accepted" -> OrderStatus.Accepted
        | "Replaced" -> OrderStatus.Replaced
        | _ -> failwithf "Unknown order status: %s" status
    
    member private _.GetOrderTypeString(orderType: StockOrderType) : string =
        match orderType with
        | StockOrderType.Limit -> "Limit"
        | StockOrderType.Market -> "Market"
        | StockOrderType.StopMarket -> "Stop"
    
    member private _.GetOrderTypeFromString(orderType: string) : StockOrderType =
        match orderType with
        | "Limit" -> StockOrderType.Limit
        | "Market" -> StockOrderType.Market
        | "Stop" -> StockOrderType.StopMarket
        | "NetCredit" -> StockOrderType.Limit
        | "NetDebit" -> StockOrderType.Limit
        | _ -> failwithf "Unknown order type: %s" orderType
    
    member private _.GetOrderInstructionString(instruction: StockOrderInstruction) : string =
        match instruction with
        | StockOrderInstruction.Buy -> "Buy"
        | StockOrderInstruction.Sell -> "Sell"
        | StockOrderInstruction.BuyToCover -> "BuyToCover"
        | StockOrderInstruction.SellShort -> "SellShort"
    
    member private _.GetOrderInstructionFromString(instruction: string) : StockOrderInstruction =
        match instruction with
        | "Buy" -> StockOrderInstruction.Buy
        | "Sell" -> StockOrderInstruction.Sell
        | "BuyToCover" -> StockOrderInstruction.BuyToCover
        | "SellShort" -> StockOrderInstruction.SellShort
        | "BuyToOpen" -> StockOrderInstruction.Buy
        | "BuyToClose" -> StockOrderInstruction.Buy
        | "SellToOpen" -> StockOrderInstruction.Sell
        | "SellToClose" -> StockOrderInstruction.Sell
        | _ -> failwithf "Unknown order instruction: %s" instruction
    
    member private _.GetAssetTypeString(assetType: AssetType) : string =
        match assetType with
        | AssetType.Equity -> "Equity"
        | AssetType.Option -> "Option"
        | AssetType.ETF -> "Etf"
    
    member private _.GetAssetTypeFromString(assetType: string) : AssetType =
        match assetType with
        | "Equity" -> AssetType.Equity
        | "Option" -> AssetType.Option
        | "Etf" -> AssetType.ETF
        | _ -> failwithf "Unknown asset type: %s" assetType
    
    member private _.GetTransactionTypeString(transactionType: AccountTransactionType) : string =
        match transactionType with
        | AccountTransactionType.Dividend -> "Dividend"
        | AccountTransactionType.Trade -> "Trade"
        | AccountTransactionType.Fee -> "Fee"
        | AccountTransactionType.Interest -> "Interest"
        | AccountTransactionType.Other -> "Other"
        | AccountTransactionType.Transfer -> "Transfer"
    
    member private _.GetTransactionTypeFromString(transactionType: string) : AccountTransactionType option =
        match transactionType with
        | null -> None
        | "Dividend" -> Some AccountTransactionType.Dividend
        | "Trade" -> Some AccountTransactionType.Trade
        | "Fee" -> Some AccountTransactionType.Fee
        | "Interest" -> Some AccountTransactionType.Interest
        | "Other" -> Some AccountTransactionType.Other
        | "Transfer" -> Some AccountTransactionType.Transfer
        | _ -> failwithf "Unknown transaction type: %s" transactionType
    
    interface IAccountStorage with
        
        member this.GetUser(userId: UserId) = 
            task {
                let storage = this :> IAggregateStorage
                let! events = storage.GetEventsAsync(userEntity, userId)
                
                let user = User(events)
                return if user.Id = Guid.Empty then None else Some user
            }
        
        member this.GetUserByEmail(emailAddress: string) = 
            task {
                let email = emailAddress.ToLowerInvariant()
                use db = this.GetConnection()
                
                let query = "SELECT id FROM users WHERE email = @emailAddress"
                let! identifier = db.QuerySingleOrDefaultAsync<string>(query, {| emailAddress = email |})
                
                if isNull identifier then
                    return None
                else
                    let userId = UserId (Guid(identifier))
                    let storage = this :> IAccountStorage
                    return! storage.GetUser(userId)
            }
        
        member this.Save(user: User) = 
            task {
                use db = this.GetConnection()
                use tx = db.BeginTransaction()
                    
                try
                    
                    let query = "INSERT INTO users (id, email) VALUES (@id, @email) ON CONFLICT DO NOTHING;"
                    do! db.ExecuteAsync(query, {| id = user.State.Id.ToString(); email = user.State.Email |}) :> Task
                    
                    let storage = this :> IAggregateStorage
                    let userId = UserId user.State.Id
                    do! storage.SaveEventsAsync(user, userEntity, userId, tx)
                    
                    tx.Commit()
                with ex ->
                    try tx.Rollback() with _ -> ()
                    raise ex
            } :> Task
        
        member this.Delete(user: User) = 
            task {
                
                use db = this.GetConnection()
                use tx = db.BeginTransaction()

                try
                    
                    let query = "DELETE FROM users WHERE id = @id"
                    do! db.ExecuteAsync(query, {| id = user.Id.ToString() |}) :> Task
                    
                    let storage = this :> IAggregateStorage
                    let userId = UserId user.Id
                    do! storage.DeleteAggregates(userEntity, userId, tx)
                    
                    tx.Commit()
                with ex ->
                    try tx.Rollback() with _ -> ()
                    raise ex
            }
        
        member this.SaveUserAssociation(association: ProcessIdToUserAssociation) = 
            task {
                use db = this.GetConnection()
                let (UserId userId) = association.UserId
                
                let query = "INSERT INTO processidtouserassociations (id, userId, timestamp) VALUES (@id, @userId, @timestamp)"
                return! db.ExecuteAsync(query, {| id = association.Id; userId = userId; timestamp = association.Timestamp |})
            }
        
        member this.GetAccountBalancesSnapshots start ``end`` userId = 
            task {
                use db = this.GetConnection()
                let (UserId id) = userId
                
                let query = "SELECT cash,equity,longValue,shortValue,date FROM accountbalancessnapshots WHERE userId = @userId AND date BETWEEN @start AND @end ORDER BY date DESC"
                let! result = db.QueryAsync<AccountBalancesSnapshot>(
                    query,
                    {| userId = id
                       start = start.ToString("yyyy-MM-dd")
                       ``end`` = ``end``.ToString("yyyy-MM-dd") |})
                
                return result
            }
        
        member this.SaveAccountBalancesSnapshot userId balances = 
            task {
                use db = this.GetConnection()
                let (UserId id) = userId
                
                let query = """INSERT INTO accountbalancessnapshots (cash,equity,longValue,shortValue,date,userId) VALUES (@cash,@equity,@longValue,@shortValue,@date,@userId)
ON CONFLICT (userId, date) DO UPDATE SET cash = @cash, equity = @equity, longValue = @longValue, shortValue = @shortValue"""
                
                return! db.ExecuteAsync(
                    query,
                    {| cash = balances.Cash
                       equity = balances.Equity
                       longValue = balances.LongValue
                       shortValue = balances.ShortValue
                       date = balances.Date
                       userId = id |})
            }
        
        member this.GetAccountBrokerageOrders(userId: UserId) = 
            task {
                use db = this.GetConnection()
                let (UserId id) = userId
                
                let query = "SELECT orderid,price,quantity,status,ticker,ordertype,instruction,assettype,executiontime,enteredtime,expirationtime,canbecancelled FROM accountbrokerageorders WHERE userId = @userId"
                let! result = db.QueryAsync<BrokerageOrderDto>(query, {| userId = id |})
                
                return result.Select(fun r ->
                    {
                        OrderId = r.orderid
                        Price = r.price
                        Quantity = r.quantity
                        Status = this.GetOrderStatusFromString(r.status)
                        StatusDescription = None
                        Ticker = Ticker(r.ticker)
                        Type = this.GetOrderTypeFromString(r.ordertype)
                        Instruction = this.GetOrderInstructionFromString(r.instruction)
                        ExecutionTime = if isNull r.executiontime then None else Some (DateTimeOffset.Parse(r.executiontime))
                        EnteredTime = if isNull r.enteredtime then None else Some (DateTimeOffset.Parse(r.enteredtime))
                        ExpirationTime = if isNull r.expirationtime then None else Some (DateTimeOffset.Parse(r.expirationtime))
                        CanBeCancelled = r.canbecancelled
                    } : StockOrder
                )
            }
        
        member this.SaveAccountBrokerageStockOrders userId orders = 
            task {
                let (UserId id) = userId
                use db = this.GetConnection()
                use tx = db.BeginTransaction()
                try
                    let query = """INSERT INTO accountbrokerageorders (orderid,price,quantity,status,ticker,ordertype,instruction,assettype,executiontime,enteredtime,expirationtime,canbecancelled,userId,modified)
                    VALUES (@orderid,@price,@quantity,@status,@ticker,@ordertype,@instruction,@assettype,@executiontime,@enteredtime,@expirationtime,@canbecancelled,@userId,@modified)
ON CONFLICT (userId, orderid) DO UPDATE SET price = @price, quantity = @quantity, status = @status, ticker = @ticker, ordertype = @ordertype, instruction = @instruction, assettype = @assettype, executiontime = @executiontime, enteredtime = @enteredtime, expirationtime = @expirationtime, canbecancelled = @canbecancelled, modified = @modified"""
                    
                    for order in orders do
                        do! db.ExecuteAsync(
                            query,
                            {| orderid = order.OrderId
                               price = order.Price
                               quantity = order.Quantity
                               status = this.GetOrderStatusString(order.Status)
                               ticker = order.Ticker.Value
                               ordertype = this.GetOrderTypeString(order.Type)
                               instruction = this.GetOrderInstructionString(order.Instruction)
                               assettype = this.GetAssetTypeString(AssetType.Equity)
                               executiontime = order.ExecutionTime |> Option.map (fun dt -> dt.ToString("u")) |> Option.toObj
                               enteredtime = order.EnteredTime |> Option.map (fun dt -> dt.ToString("u")) |> Option.toObj
                               expirationtime = order.ExpirationTime |> Option.map (fun dt -> dt.ToString("u")) |> Option.toObj
                               canbecancelled = order.CanBeCancelled
                               userId = id
                               modified = DateTimeOffset.UtcNow |}) :> Task
                    
                    tx.Commit()
                with ex ->
                    try tx.Rollback() with _ -> ()
                    raise ex
            } :> Task
        
        member this.SaveAccountBrokerageOptionOrders userId orders = 
            Task.CompletedTask
        
        member this.InsertAccountBrokerageTransactions userId transactions = 
            task {
                let (UserId id) = userId
                use db = this.GetConnection()
                use tx = db.BeginTransaction()
                
                try
                    let query = """INSERT INTO accountbrokeragetransactions (transactionid, description, brokeragetype, tradedate, settlementdate, netamount, inferredticker, inferredtype, userid, inserted, applied)
        VALUES (@TransactionId, @Description, @BrokerageType, @TradeDate, @SettlementDate, @NetAmount, @InferredTicker, @InferredType, @UserId, @Inserted, @Applied) ON CONFLICT (userid, transactionid) DO NOTHING"""
                    
                    for transaction in transactions do
                        do! db.ExecuteAsync(
                            query,
                            {| TransactionId = transaction.TransactionId
                               Description = transaction.Description
                               BrokerageType = transaction.BrokerageType
                               TradeDate = transaction.TradeDate.ToString("u")
                               SettlementDate = transaction.SettlementDate.ToString("u")
                               NetAmount = transaction.NetAmount
                               InferredTicker = transaction.InferredTicker |> Option.map (fun t -> t.Value) |> Option.toObj
                               InferredType = transaction.InferredType |> Option.map this.GetTransactionTypeString |> Option.toObj
                               UserId = id
                               Inserted = DateTimeOffset.UtcNow.ToString("u")
                               Applied = transaction.Applied |> Option.map (fun dt -> dt.ToString("u")) |> Option.toObj |}) :> Task
                    
                    tx.Commit()
                with ex ->
                    try tx.Rollback() with _ -> ()
                    raise ex
            } :> Task
        
        member this.SaveAccountBrokerageTransactions userId transactions = 
            task {
                let (UserId id) = userId
                
                use db = this.GetConnection()
                use tx = db.BeginTransaction()

                try 
                    let query = """INSERT INTO accountbrokeragetransactions (transactionid, description, brokeragetype, tradedate, settlementdate, netamount, inferredticker, inferredtype, userid, inserted, applied)
        VALUES (@TransactionId, @Description, @BrokerageType, @TradeDate, @SettlementDate, @NetAmount, @InferredTicker, @InferredType, @UserId, @Inserted, @Applied) 
        ON CONFLICT (userid, transactionid) DO UPDATE 
        SET description = @Description, 
            brokeragetype = @BrokerageType, 
            tradedate = @TradeDate, 
            settlementdate = @SettlementDate, 
            netamount = @NetAmount, 
            inferredticker = @InferredTicker,
            inferredtype = @InferredType,
            inserted = @Inserted, 
            applied = @Applied"""
                    
                    for transaction in transactions do
                        do! db.ExecuteAsync(
                            query,
                            {| TransactionId = transaction.TransactionId
                               Description = transaction.Description
                               BrokerageType = transaction.BrokerageType
                               TradeDate = transaction.TradeDate.ToString("u")
                               SettlementDate = transaction.SettlementDate.ToString("u")
                               NetAmount = transaction.NetAmount
                               InferredTicker = transaction.InferredTicker |> Option.map (fun t -> t.Value) |> Option.toObj
                               InferredType = transaction.InferredType |> Option.map this.GetTransactionTypeString |> Option.toObj
                               UserId = id
                               Inserted = DateTimeOffset.UtcNow.ToString("u")
                               Applied = transaction.Applied |> Option.map (fun dt -> dt.ToString("u")) |> Option.toObj |}) :> Task
                    
                    tx.Commit()
                with ex ->
                    try tx.Rollback() with _ -> ()
                    raise ex
            } :> Task
        
        member this.GetAccountBrokerageTransactions(userId: UserId) = 
            task {
                use db = this.GetConnection()
                let (UserId id) = userId
                
                let query = "SELECT transactionid, description, brokeragetype, tradedate, settlementdate, netamount, inferredticker, inferredtype, inserted, applied FROM accountbrokeragetransactions WHERE userid = @userId"
                let! result = db.QueryAsync<BrokerageTransactionDto>(query, {| userId = id |})
                
                return result.Select(fun r ->
                    {
                        TransactionId = r.transactionid
                        Description = r.description
                        TradeDate = DateTimeOffset.Parse(r.tradedate)
                        SettlementDate = DateTimeOffset.Parse(r.settlementdate)
                        NetAmount = r.netamount
                        BrokerageType = r.brokeragetype
                        InferredType = this.GetTransactionTypeFromString(r.inferredtype)
                        InferredTicker = if isNull r.inferredticker then None else Some (Ticker(r.inferredticker))
                        Inserted = if isNull r.inserted then None else Some (DateTimeOffset.Parse(r.inserted))
                        Applied = if isNull r.applied then None else Some (DateTimeOffset.Parse(r.applied))
                    } : AccountTransaction
                )
            }
        
        member this.GetUserAssociation(id: Guid) = 
            task {
                use db = this.GetConnection()
                
                let query = "SELECT * FROM processidtouserassociations WHERE id = @id"
                let! result = db.QuerySingleOrDefaultAsync<ProcessIdToUserAssociation>(query, {| id = id |})
                
                return if isNull (box result) then None else Some result
            }
        
        member this.GetUserEmailIdPairs() = 
            task {
                use db = this.GetConnection()
                
                let! users = db.QueryAsync<EmailIdPair>("SELECT email,id FROM users")
                return users
            }
        
        member this.GetOptionPricing userId symbol = 
            let rec readRows (reader: System.Data.IDataReader) acc =
                if reader.Read() then
                    let pricing = {
                        UserId = UserId (reader.GetGuid(reader.GetOrdinal("userId")))
                        OptionPositionId = OptionPositionId (reader.GetGuid(reader.GetOrdinal("optionPositionId")))
                        UnderlyingTicker = Ticker(reader.GetString(reader.GetOrdinal("underlyingTicker")))
                        Symbol = OptionTicker (reader.GetString(reader.GetOrdinal("symbol")))
                        Expiration = OptionExpiration.create(reader.GetString(reader.GetOrdinal("expiration")))
                        StrikePrice = reader.GetDecimal(reader.GetOrdinal("strikePrice"))
                        OptionType = OptionType.FromString(reader.GetString(reader.GetOrdinal("optionType")))
                        Volume = reader.GetInt32(reader.GetOrdinal("volume"))
                        OpenInterest = reader.GetInt32(reader.GetOrdinal("openInterest"))
                        Bid = reader.GetDecimal(reader.GetOrdinal("bid"))
                        Ask = reader.GetDecimal(reader.GetOrdinal("ask"))
                        Last = reader.GetDecimal(reader.GetOrdinal("last"))
                        Mark = reader.GetDecimal(reader.GetOrdinal("mark"))
                        Volatility = reader.GetDecimal(reader.GetOrdinal("volatility"))
                        Delta = reader.GetDecimal(reader.GetOrdinal("delta"))
                        Gamma = reader.GetDecimal(reader.GetOrdinal("gamma"))
                        Theta = reader.GetDecimal(reader.GetOrdinal("theta"))
                        Vega = reader.GetDecimal(reader.GetOrdinal("vega"))
                        Rho = reader.GetDecimal(reader.GetOrdinal("rho"))
                        UnderlyingPrice = Some (reader.GetDecimal(reader.GetOrdinal("underlyingPrice")))
                        Timestamp = DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("timestamp")))
                    }
                    readRows reader (pricing :: acc)
                else
                    List.rev acc
            task {
                use db = this.GetConnection()
                let (UserId id) = userId
                let (OptionTicker symbolValue) = symbol
                
                let query = "SELECT * FROM optionpricings WHERE userid = @userId AND symbol = @symbol ORDER BY timestamp ASC"
                use! reader = db.ExecuteReaderAsync(query, {| userId = id; symbol = symbolValue |})
                
                return readRows reader [] |> Seq.ofList
            }
        
        member this.SaveOptionPricing pricing userId = 
            task {
                use db = this.GetConnection()
                let (UserId id) = userId
                let (OptionPositionId optionPositionId) = pricing.OptionPositionId
                let (OptionTicker symbolValue) = pricing.Symbol
                
                let query = """INSERT INTO optionpricings (userid, optionpositionid, underlyingticker, symbol, expiration, strikeprice, optiontype, volume, openinterest, bid, ask, last, mark, volatility, delta, gamma, theta, vega, rho, underlyingprice, timestamp)
VALUES (@userid, @optionpositionid, @underlyingticker, @symbol, @expiration, @strikeprice, @optiontype, @volume, @openinterest, @bid, @ask, @last, @mark, @volatility, @delta, @gamma, @theta, @vega, @rho, @underlyingprice, @timestamp)
            """
                
                do! db.ExecuteAsync(
                    query,
                    {| userId = id
                       optionpositionid = optionPositionId
                       underlyingticker = pricing.UnderlyingTicker.Value
                       symbol = symbolValue
                       expiration = pricing.Expiration.ToString()
                       strikeprice = pricing.StrikePrice
                       optiontype = pricing.OptionType.ToString()
                       volume = pricing.Volume
                       openinterest = pricing.OpenInterest
                       bid = pricing.Bid
                       ask = pricing.Ask
                       last = pricing.Last
                       mark = pricing.Mark
                       volatility = pricing.Volatility
                       delta = pricing.Delta
                       gamma = pricing.Gamma
                       theta = pricing.Theta
                       vega = pricing.Vega
                       rho = pricing.Rho
                       underlyingprice = pricing.UnderlyingPrice |> Option.toNullable
                       timestamp = pricing.Timestamp |}) :> Task
            }
        
        member this.GetStockPriceAlerts(userId: UserId) = 
            task {
                use db = this.GetConnection()
                let (UserId id) = userId
                
                let query = "SELECT alertid, userid, ticker, pricelevel, alerttype, note, state, createdat, triggeredat, lastresetat FROM stockpricealerts WHERE userid = @userId ORDER BY createdat DESC"
                let! result = db.QueryAsync<StockPriceAlertDto>(query, {| userId = id |})
                
                return result.Select(fun r ->
                    {
                        AlertId = r.alertid
                        UserId = UserId r.userid
                        Ticker = Ticker(r.ticker)
                        PriceLevel = r.pricelevel
                        AlertType = PriceAlertType.fromString(r.alerttype)
                        Note = r.note
                        State = PriceAlertState.fromString(r.state)
                        CreatedAt = r.createdat
                        TriggeredAt = if r.triggeredat.HasValue then Some r.triggeredat.Value else None
                        LastResetAt = if r.lastresetat.HasValue then Some r.lastresetat.Value else None
                    } : StockPriceAlert
                )
            }
        
        member this.SaveStockPriceAlert(alert: StockPriceAlert) = 
            task {
                use db = this.GetConnection()
                let (UserId userId) = alert.UserId
                
                let query = """
INSERT INTO stockpricealerts (alertid, userid, ticker, pricelevel, alerttype, note, state, createdat, triggeredat, lastresetat)
VALUES (@alertid, @userid, @ticker, @pricelevel, @alerttype, @note, @state, @createdat, @triggeredat, @lastresetat)
ON CONFLICT (alertid) DO UPDATE SET
    pricelevel = EXCLUDED.pricelevel,
    alerttype = EXCLUDED.alerttype,
    note = EXCLUDED.note,
    state = EXCLUDED.state,
    triggeredat = EXCLUDED.triggeredat,
    lastresetat = EXCLUDED.lastresetat
            """
                
                return! db.ExecuteAsync(
                    query,
                    {| alertid = alert.AlertId
                       userid = userId
                       ticker = alert.Ticker.Value
                       pricelevel = alert.PriceLevel
                       alerttype = PriceAlertType.toString alert.AlertType
                       note = alert.Note
                       state = PriceAlertState.toString alert.State
                       createdat = alert.CreatedAt
                       triggeredat = alert.TriggeredAt |> Option.toNullable
                       lastresetat = alert.LastResetAt |> Option.toNullable |})
            }
        
        member this.DeleteStockPriceAlert alertId userId = 
            task {
                use db = this.GetConnection()
                let (UserId id) = userId
                
                let query = "DELETE FROM stockpricealerts WHERE alertid = @alertid AND userid = @userid"
                return! db.ExecuteAsync(query, {| alertid = alertId; userid = id |})
            }
        
        member this.GetReminders(userId: UserId) = 
            task {
                use db = this.GetConnection()
                let (UserId id) = userId
                
                let query = "SELECT reminderid, userid, date, message, ticker, state, createdat, sentat FROM reminders WHERE userid = @userId ORDER BY date ASC"
                let! result = db.QueryAsync<ReminderDto>(query, {| userId = id |})
                
                return result.Select(fun r ->
                    {
                        ReminderId = r.reminderid
                        UserId = UserId r.userid
                        Date = r.date
                        Message = r.message
                        Ticker = if isNull r.ticker then None else Some (Ticker(r.ticker))
                        State = ReminderState.fromString(r.state)
                        CreatedAt = r.createdat
                        SentAt = if r.sentat.HasValue then Some r.sentat.Value else None
                    } :Reminder
                )
            }
        
        member this.SaveReminder(reminder: Reminder) = 
            task {
                use db = this.GetConnection()
                let (UserId userId) = reminder.UserId
                
                let query = """
INSERT INTO reminders (reminderid, userid, date, message, ticker, state, createdat, sentat)
VALUES (@reminderid, @userid, @date, @message, @ticker, @state, @createdat, @sentat)
ON CONFLICT (reminderid) DO UPDATE SET
    date = EXCLUDED.date,
    message = EXCLUDED.message,
    ticker = EXCLUDED.ticker,
    state = EXCLUDED.state,
    sentat = EXCLUDED.sentat
            """
                
                return! db.ExecuteAsync(
                    query,
                    {| reminderid = reminder.ReminderId
                       userid = userId
                       date = reminder.Date.ToUniversalTime()
                       message = reminder.Message
                       ticker = reminder.Ticker |> Option.map (fun t -> t.Value) |> Option.toObj
                       state = ReminderState.toString reminder.State
                       createdat = reminder.CreatedAt.ToUniversalTime()
                       sentat = reminder.SentAt |> Option.map (fun dt -> dt.ToUniversalTime()) |> Option.toNullable |})
            }
        
        member this.DeleteReminder reminderId userId = 
            task {
                use db = this.GetConnection()
                let (UserId id) = userId
                
                let query = "DELETE FROM reminders WHERE reminderid = @reminderid AND userid = @userid"
                return! db.ExecuteAsync(query, {| reminderid = reminderId; userid = id |})
            }
        
        member this.DeleteSentRemindersBefore(cutoffDate: DateTimeOffset) =
            task {
                use db = this.GetConnection()
                let sentState = ReminderState.toString ReminderState.Sent
                let query = "DELETE FROM reminders WHERE state = @state AND sentat < @cutoffDate"
                return! db.ExecuteAsync(query, {| state = sentState; cutoffDate = cutoffDate.ToUniversalTime() |})
            }
        
        member this.GetTickerCik(ticker: string) = 
            task {
                use db = this.GetConnection()
                
                let query = "SELECT ticker, cik, title, lastupdated FROM tickercik WHERE ticker = @ticker"
                let! result = db.QuerySingleOrDefaultAsync<TickerCikMapping>(query, {| ticker = ticker.ToUpperInvariant() |})
                
                return if isNull (box result) then None else Some result
            }
        
        member this.SaveTickerCikMappings(mappings: TickerCikMapping seq) = 
            task {
                use db = this.GetConnection()
                use tx = db.BeginTransaction()
                try
                    
                    
                    let query = """
INSERT INTO tickercik (ticker, cik, title, lastupdated)
VALUES (@ticker, @cik, @title, @lastupdated)
ON CONFLICT (ticker) DO UPDATE SET
    cik = EXCLUDED.cik,
    title = EXCLUDED.title,
    lastupdated = EXCLUDED.lastupdated
                """
                    
                    let! _ = db.ExecuteAsync(query, mappings, transaction = tx)
                    
                    tx.Commit()
                with ex ->
                    try tx.Rollback() with _ -> ()
                    raise ex
            }
        
        member this.GetAllTickerCikMappings() = 
            task {
                use db = this.GetConnection()
                
                let query = "SELECT ticker, cik, title, lastupdated FROM tickercik ORDER BY ticker"
                let! result = db.QueryAsync<TickerCikMapping>(query)
                return result
            }
        
        member this.GetTickerCikLastUpdated() = 
            task {
                use db = this.GetConnection()
                
                let query = "SELECT MAX(lastupdated) FROM tickercik"
                let! result = db.QuerySingleOrDefaultAsync<Nullable<DateTime>>(query)
                
                return if result.HasValue then Some (DateTimeOffset(result.Value, TimeSpan.Zero)) else None
            }
        
        member this.SearchTickerCik(query: string) = 
            task {
                use db = this.GetConnection()
                
                let sql = """
                SELECT ticker, cik, title, lastupdated 
                FROM tickercik 
                WHERE UPPER(ticker) LIKE UPPER(@query) OR UPPER(title) LIKE UPPER(@query)
                ORDER BY ticker
                LIMIT 50
            """
                
                let searchPattern = sprintf "%%%s%%" query
                let! result = db.QueryAsync<TickerCikMapping>(sql, {| query = searchPattern |})
                return result
            }

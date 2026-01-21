using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using core.fs.Accounts;
using core.fs.Adapters.Brokerage;
using core.fs.Adapters.Storage;
using core.fs.Alerts;
using core.fs.Options;
using core.Shared;
using Dapper;
using Microsoft.FSharp.Core;
using storage.shared;

namespace storage.postgres
{
    public class AccountStorage : PostgresAggregateStorage, IAccountStorage
    {
        private const string _user_entity = "users";

        public AccountStorage(IOutbox outbox, string cnn) : base(outbox, cnn)
        {
        }

        public async Task<FSharpOption<User>> GetUser(UserId userId)
        {
            var events = await GetEventsAsync(_user_entity, userId);

            var u = new User(events);
            return u.Id == Guid.Empty ? FSharpOption<User>.None : new FSharpOption<User>(u);
        }

        public async Task<FSharpOption<User>> GetUserByEmail(string emailAddress)
        {
            emailAddress = emailAddress.ToLowerInvariant();

            string? identifier;
            using(var db = GetConnection())
            {
                var query = @"SELECT id FROM users WHERE email = :emailAddress";

                identifier = await db.QuerySingleOrDefaultAsync<string>(query, new {emailAddress});
            }

            if (identifier == null)
            {
                return FSharpOption<User>.None;
            }

            return await GetUser(UserId.NewUserId(new Guid(identifier)));
        }

        public async Task Save(User u)
        {
            using var db = GetConnection();
            using var tx = db.BeginTransaction();
            
            try
            {
                var query = @"INSERT INTO users (id, email) VALUES (:id, :email) ON CONFLICT DO NOTHING;";

                await db.ExecuteAsync(query, new {id = u.State.Id.ToString(), email = u.State.Email});

                await SaveEventsAsync(u, _user_entity, UserId.NewUserId(u.State.Id), outsideTransaction: tx);
                
                tx.Commit();
            }
            catch
            {
                tx?.Rollback();
                throw;
            }
        }

        public async Task Delete(User user)
        {
            using var db = GetConnection();
            using var tx = db.BeginTransaction();
            
            try
            {
                var query = @"DELETE FROM users WHERE id = :id";
                await db.ExecuteAsync(query, new {id = user.Id.ToString()});
                
                await DeleteAggregates(_user_entity, UserId.NewUserId(user.Id), outsideTransaction: tx);
                
                tx.Commit();
            }
            catch
            {
                tx?.Rollback();
                throw;
            }
        }

        public async Task SaveUserAssociation(ProcessIdToUserAssociation r)
        {
            using var db = GetConnection();
            
            var query = @"INSERT INTO processidtouserassociations (id, userId, timestamp) VALUES (:id, :userId, :timestamp)";

            await db.ExecuteAsync(query, new {r.Id, userId = r.UserId.Item, timestamp = r.Timestamp});
        }

        public async Task<IEnumerable<AccountBalancesSnapshot>> GetAccountBalancesSnapshots(DateTimeOffset start, DateTimeOffset end, UserId userId)
        {
            using var db = GetConnection();

            var query = @"SELECT cash,equity,longValue,shortValue,date FROM accountbalancessnapshots WHERE userId = :userId AND date BETWEEN :start AND :end ORDER BY date DESC";
            
            var result = await db.QueryAsync<AccountBalancesSnapshot>(
                query, 
                new
                {
                    userId = userId.Item,
                    start = start.ToString("yyyy-MM-dd"),
                    end = end.ToString("yyyy-MM-dd")
                });
            
            return result;
        }

        public async Task SaveAccountBalancesSnapshot(UserId userId, AccountBalancesSnapshot balances)
        {
            using var db = GetConnection();
            
            var query = @"INSERT INTO accountbalancessnapshots (cash,equity,longValue,shortValue,date,userId) VALUES (:cash,:equity,:longValue,:shortValue,:date,:userId)
ON CONFLICT (userId, date) DO UPDATE SET cash = :cash, equity = :equity, longValue = :longValue, shortValue = :shortValue";
            
            await db.ExecuteAsync(query, new
            {
                cash = balances.Cash,
                equity = balances.Equity,
                longValue = balances.LongValue,
                shortValue = balances.ShortValue,
                date = balances.Date,
                userId = userId.Item
            });
        }
        
        private string GetOrderStatusString(OrderStatus status)
        {
            //| Filled | Working | PendingActivation | Expired | Canceled | Rejected
            return status.Tag switch
            {
                OrderStatus.Tags.Filled => "Filled",
                OrderStatus.Tags.Working => "Working",
                OrderStatus.Tags.PendingActivation => "PendingActivation",
                OrderStatus.Tags.Expired => "Expired",
                OrderStatus.Tags.Canceled => "Canceled",
                OrderStatus.Tags.Rejected => "Rejected",
                OrderStatus.Tags.Accepted => "Accepted",
                OrderStatus.Tags.Replaced => "Replaced",
                _ => throw new Exception($"Unknown order status: {status}")
            };
        }
        private static OrderStatus GetOrderStatusFromString(string status) =>
            status switch
            {
                "Filled" => OrderStatus.Filled,
                "Working" => OrderStatus.Working,
                "PendingActivation" => OrderStatus.PendingActivation,
                "Expired" => OrderStatus.Expired,
                "Canceled" => OrderStatus.Canceled,
                "Rejected" => OrderStatus.Rejected,
                "Accepted" => OrderStatus.Accepted,
                "Replaced" => OrderStatus.Replaced,
                _ => throw new Exception($"Unknown order status: {status}")
            };

        private static string GetOrderTypeString(StockOrderType orderType) =>
            orderType.Tag switch
            {
                StockOrderType.Tags.Limit => "Limit",
                StockOrderType.Tags.Market => "Market",
                StockOrderType.Tags.StopMarket => "Stop",
                _ => throw new Exception($"Unknown order type: {orderType}")
            };
        private static StockOrderType GetOrderTypeFromString(string orderType) =>
            orderType switch
            {
                "Limit" => StockOrderType.Limit,
                "Market" => StockOrderType.Market,
                "Stop" => StockOrderType.StopMarket,
                "NetCredit" => StockOrderType.Limit,
                "NetDebit" => StockOrderType.Limit,
                _ => throw new Exception($"Unknown order type: {orderType}")
            };
        
        private static string GetOrderInstructionString(StockOrderInstruction instruction) =>
            instruction.Tag switch
            {
                StockOrderInstruction.Tags.Buy => "Buy",
                StockOrderInstruction.Tags.Sell => "Sell",
                StockOrderInstruction.Tags.BuyToCover => "BuyToCover",
                StockOrderInstruction.Tags.SellShort => "SellShort",
                _ => throw new Exception($"Unknown order instruction: {instruction}")
            };
        private static StockOrderInstruction GetOrderInstructionFromString(string instruction) =>
            instruction switch
            {
                "Buy" => StockOrderInstruction.Buy,
                "Sell" => StockOrderInstruction.Sell,
                "BuyToCover" => StockOrderInstruction.BuyToCover,
                "SellShort" => StockOrderInstruction.SellShort,
                "BuyToOpen" => StockOrderInstruction.Buy,
                "BuyToClose" => StockOrderInstruction.Buy,
                "SellToOpen" => StockOrderInstruction.Sell,
                "SellToClose" => StockOrderInstruction.Sell,
                _ => throw new Exception($"Unknown order instruction: {instruction}")
            };

        private static string GetAssetTypeString(AssetType assetType) =>
            assetType.Tag switch
            {
                AssetType.Tags.Equity => "Equity",
                AssetType.Tags.Option => "Option",
                AssetType.Tags.ETF => "Etf",
                _ => throw new Exception($"Unknown asset type: {assetType}")
            };
        private static AssetType GetAssetTypeFromString(string assetType) =>
            assetType switch
            {
                "Equity" => AssetType.Equity,
                "Option" => AssetType.Option,
                "Etf" => AssetType.ETF,
                _ => throw new Exception($"Unknown asset type: {assetType}")
            };

        public async Task SaveAccountBrokerageStockOrders(UserId userId, IEnumerable<StockOrder> orders)
        {
            using var db = GetConnection();
            
            var query = @"INSERT INTO accountbrokerageorders (orderid,price,quantity,status,ticker,ordertype,instruction,assettype,executiontime,enteredtime,expirationtime,canbecancelled,userId,modified)
                        VALUES (:orderid,:price,:quantity,:status,:ticker,:ordertype,:instruction,:assettype,:executiontime,:enteredtime,:expirationtime,:canbecancelled,:userId,:modified)
ON CONFLICT (userId, orderid) DO UPDATE SET price = :price, quantity = :quantity, status = :status, ticker = :ticker, ordertype = :ordertype, instruction = :instruction, assettype = :assettype, executiontime = :executiontime, enteredtime = :enteredtime, expirationtime = :expirationtime, canbecancelled = :canbecancelled, modified = :modified";

            using var tx = db.BeginTransaction();
            
            try
            {
                foreach (var order in orders)
                {
                    await db.ExecuteAsync(query, new
                    {
                        orderid = order.OrderId,
                        price = order.Price,
                        quantity = order.Quantity,
                        status = GetOrderStatusString(order.Status),
                        ticker = order.Ticker.Value,
                        ordertype = GetOrderTypeString(order.Type),
                        instruction = GetOrderInstructionString(order.Instruction),
                        assettype = GetAssetTypeString(AssetType.Equity),
                        executiontime = FSharpOption<DateTimeOffset>.get_IsNone(order.ExecutionTime) ? null : order.ExecutionTime.Value.ToString("u"),
                        enteredtime = FSharpOption<DateTimeOffset>.get_IsNone(order.EnteredTime) ? null : order.EnteredTime.Value.ToString("u"),
                        expirationtime = FSharpOption<DateTimeOffset>.get_IsNone(order.ExpirationTime) ? null : order.ExpirationTime.Value.ToString("u"),
                        canbecancelled = order.CanBeCancelled,
                        userId = userId.Item,
                        modified = DateTimeOffset.UtcNow
                    });
                }
                tx.Commit();
            }
            catch
            {
                tx?.Rollback();
                throw;
            }
        }

        public Task SaveAccountBrokerageOptionOrders(UserId userId, IEnumerable<OptionOrder> orders)
        {
            // TODO: Implement
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<StockOrder>> GetAccountBrokerageOrders(UserId userId)
        {
            using var db = GetConnection();
            
            var query = @"SELECT orderid,price,quantity,status,ticker,ordertype,instruction,assettype,executiontime,enteredtime,expirationtime,canbecancelled FROM accountbrokerageorders WHERE userId = :userId";

            var result = await db.QueryAsync(
                query,
                new
                {
                    userId = userId.Item
                });
            
            return result.Select(r =>
                new StockOrder
                {
                    OrderId = r.orderid,
                    Instruction = GetOrderInstructionFromString(r.instruction),
                    Price = r.price,
                    Quantity = r.quantity,
                    Status = GetOrderStatusFromString(r.status),
                    Ticker = new Ticker(r.ticker),
                    Type = GetOrderTypeFromString(r.ordertype),
                    ExecutionTime = r.executiontime == null ? FSharpOption<DateTimeOffset>.None : new FSharpOption<DateTimeOffset>(DateTimeOffset.Parse(r.executiontime)),
                    EnteredTime = DateTimeOffset.Parse(r.enteredtime),
                    ExpirationTime = r.expirationtime == null ? FSharpOption<DateTimeOffset>.None : new FSharpOption<DateTimeOffset>(DateTimeOffset.Parse(r.expirationtime)),
                    CanBeCancelled = r.canbecancelled,
                });
        }

        static string GetTransactionTypeString(AccountTransactionType type)
        {
            return type.Tag switch
            {
                AccountTransactionType.Tags.Dividend => "Dividend",
                AccountTransactionType.Tags.Trade => "Trade",
                AccountTransactionType.Tags.Fee => "Fee",
                AccountTransactionType.Tags.Interest => "Interest",
                AccountTransactionType.Tags.Other => "Other",
                AccountTransactionType.Tags.Transfer => "Transfer",
                _ => throw new Exception($"Unknown transaction type: {type}")
            };
        }

        private static readonly string _accountBrokerageTransactionInsert =
            @"INSERT INTO accountbrokeragetransactions (transactionid, description, brokeragetype, tradedate, settlementdate, netamount, inferredticker, inferredtype, userid, inserted, applied)
        VALUES (@TransactionId, @Description, @BrokerageType, @TradeDate, @SettlementDate, @NetAmount, @InferredTicker, @InferredType, @UserId, @Inserted, @Applied)";

        private static object ToAccountBrokerageTransactionRecord(UserId userId, AccountTransaction transaction)
            => new
            {
                transaction.TransactionId,
                transaction.Description,
                transaction.BrokerageType,
                TradeDate = transaction.TradeDate.ToString("u"),
                SettlementDate = transaction.SettlementDate.ToString("u"),
                transaction.NetAmount,
                InferredTicker = FSharpOption<Ticker>.get_IsNone(transaction.InferredTicker)
                    ? null
                    : transaction.InferredTicker.Value.Value,
                InferredType = FSharpOption<AccountTransactionType>.get_IsNone(transaction.InferredType)
                    ? null
                    : GetTransactionTypeString(transaction.InferredType.Value),
                UserId = userId.Item,
                Inserted = DateTimeOffset.UtcNow.ToString("u"),
                Applied = FSharpOption<DateTimeOffset>.get_IsNone(transaction.Applied)
                    ? null
                    : transaction.Applied.Value.ToString("u")
            };
        
        public async Task InsertAccountBrokerageTransactions(UserId userId, IEnumerable<AccountTransaction> transactions)
        {
            using var db = GetConnection();

            var query = @$"{_accountBrokerageTransactionInsert} ON CONFLICT (userid, transactionid) DO NOTHING";
        
            using var tx = db.BeginTransaction();
        
            try
            {
                foreach (var transaction in transactions)
                {
                    await db.ExecuteAsync(query, ToAccountBrokerageTransactionRecord(userId, transaction));
                }
            
                tx.Commit();
            }
            catch
            {
                tx?.Rollback();
                throw;
            }
        }
        
        public async Task SaveAccountBrokerageTransactions(UserId userId, AccountTransaction[] transactions)
        {
            using var db = GetConnection();
        
            var query = @$"{_accountBrokerageTransactionInsert} 
        ON CONFLICT (userid, transactionid) DO UPDATE 
        SET description = @Description, 
            brokeragetype = @BrokerageType, 
            tradedate = @TradeDate, 
            settlementdate = @SettlementDate, 
            netamount = @NetAmount, 
            inferredticker = @InferredTicker,
            inferredtype = @InferredType,
            inserted = @Inserted, 
            applied = @Applied";
        
            using var tx = db.BeginTransaction();
        
            try
            {
                foreach (var transaction in transactions)
                {
                    await db.ExecuteAsync(query, ToAccountBrokerageTransactionRecord(userId, transaction));
                }
            
                tx.Commit();
            }
            catch
            {
                tx?.Rollback();
                throw;
            }
        }
        
        public async Task<IEnumerable<AccountTransaction>> GetAccountBrokerageTransactions(UserId userId)
        {
            using var db = GetConnection();
            
            var query = @"SELECT transactionid, description, brokeragetype, tradedate, settlementdate, netamount, inferredticker, inferredtype, inserted, applied FROM accountbrokeragetransactions WHERE userid = :userId";

            var result = await db.QueryAsync(query, new { userId = userId.Item});
            
            return result.Select(r =>
                new AccountTransaction(
                    r.transactionid,
                    r.description,
                    ParseWithHandling(r.tradedate),
                    ParseWithHandling(r.settlementdate),
                    r.netamount,
                    r.brokeragetype,
                    GetTransactionTypeFromString(r.inferredtype),
                    r.inferredticker == null ? FSharpOption<Ticker>.None : new FSharpOption<Ticker>(new Ticker(r.inferredticker)),
                    ParseWithHandling(r.inserted),
                    r.applied == null ? FSharpOption<DateTimeOffset>.None : new FSharpOption<DateTimeOffset>(ParseWithHandling(r.applied))
                ));

            FSharpOption<AccountTransactionType> GetTransactionTypeFromString(string type)
            {
                return type switch
                {
                    null => FSharpOption<AccountTransactionType>.None,
                    "Dividend" => AccountTransactionType.Dividend,
                    "Trade" => AccountTransactionType.Trade,
                    "Fee" => AccountTransactionType.Fee,
                    "Interest" => AccountTransactionType.Interest,
                    "Other" => AccountTransactionType.Other,
                    "Transfer" => AccountTransactionType.Transfer,
                    _ => throw new Exception($"Unknown transaction type: {type}")
                };
            }

            DateTimeOffset ParseWithHandling(string input)
            {
                if (DateTimeOffset.TryParse(input, out var result))
                {
                    return result;
                }
                else
                {
                    throw new Exception($"Could not parse {input} as a DateTimeOffset");
                }
            }
        }

        public async Task<FSharpOption<ProcessIdToUserAssociation>> GetUserAssociation(Guid id)
        {
            using var db = GetConnection();
            
            var query = @"SELECT * FROM processidtouserassociations WHERE id = :id";

            var result = await db.QuerySingleOrDefaultAsync<ProcessIdToUserAssociation>(query, new { id });
            
            return result == null ? FSharpOption<ProcessIdToUserAssociation>.None : new FSharpOption<ProcessIdToUserAssociation>(result);
        }

        public async Task<IEnumerable<EmailIdPair>> GetUserEmailIdPairs()
        {
            using var db = GetConnection();
            
            var users = await db.QueryAsync<EmailIdPair>(
                @"SELECT email,id FROM users"
            );
            
            return users;
        }

        public async Task<IEnumerable<OptionPricing>> GetOptionPricing(UserId userId, OptionTicker symbol)
        {
            using var db = GetConnection();

            const string query = @"SELECT * FROM optionpricings WHERE userid = :userId AND symbol = :symbol ORDER BY timestamp ASC";
            
            var reader = await db.ExecuteReaderAsync(query, new {userId = userId.Item, symbol = symbol.Item});
            
            var result = new List<OptionPricing>();
            
            while (reader.Read())
            {
                var pricing = new OptionPricing(
                    userId: UserId.NewUserId(reader.GetGuid(reader.GetOrdinal("userId"))),
                    optionPositionId: OptionPositionId.NewOptionPositionId(reader.GetGuid(reader.GetOrdinal("optionPositionId"))),
                    underlyingTicker: new Ticker(reader.GetString(reader.GetOrdinal("underlyingTicker"))),
                    symbol: OptionTicker.NewOptionTicker(reader.GetString(reader.GetOrdinal("symbol"))),
                    expiration: OptionExpiration.create(reader.GetString(reader.GetOrdinal("expiration"))),
                    strikePrice: reader.GetDecimal(reader.GetOrdinal("strikePrice")),
                    optionType: OptionType.FromString(reader.GetString(reader.GetOrdinal("optionType"))),
                    volume: reader.GetInt32(reader.GetOrdinal("volume")),
                    openInterest: reader.GetInt32(reader.GetOrdinal("openInterest")),
                    bid: reader.GetDecimal(reader.GetOrdinal("bid")),
                    ask: reader.GetDecimal(reader.GetOrdinal("ask")),
                    last: reader.GetDecimal(reader.GetOrdinal("last")),
                    mark: reader.GetDecimal(reader.GetOrdinal("mark")),
                    volatility: reader.GetDecimal(reader.GetOrdinal("volatility")),
                    delta: reader.GetDecimal(reader.GetOrdinal("delta")),
                    gamma: reader.GetDecimal(reader.GetOrdinal("gamma")),
                    theta: reader.GetDecimal(reader.GetOrdinal("theta")),
                    vega: reader.GetDecimal(reader.GetOrdinal("vega")),
                    rho: reader.GetDecimal(reader.GetOrdinal("rho")),
                    underlyingPrice: FSharpOption<decimal>.Some(reader.GetDecimal(reader.GetOrdinal("underlyingPrice"))),
                    timestamp: new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("timestamp")))
                );
                
                result.Add(pricing);
            }

            return result;
        }

        public async Task SaveOptionPricing(OptionPricing pricing, UserId userId)
        {
            using var db = GetConnection();

            var query =
                @"INSERT INTO optionpricings (userid, optionpositionid, underlyingticker, symbol, expiration, strikeprice, optiontype, volume, openinterest, bid, ask, last, mark, volatility, delta, gamma, theta, vega, rho, underlyingprice, timestamp)
VALUES (:userid, :optionpositionid, :underlyingticker, :symbol, :expiration, :strikeprice, :optiontype, :volume, :openinterest, :bid, :ask, :last, :mark, :volatility, :delta, :gamma, :theta, :vega, :rho, :underlyingprice, :timestamp)
            ";
            
            await db.ExecuteAsync(query, new
            {
                userId = userId.Item,
                optionpositionid = pricing.OptionPositionId.Item,
                underlyingticker = pricing.UnderlyingTicker.Value,
                symbol = pricing.Symbol.Item,
                expiration = pricing.Expiration.ToString(),
                strikeprice = pricing.StrikePrice,
                optiontype = pricing.OptionType.ToString(),
                volume = pricing.Volume,
                openinterest = pricing.OpenInterest,
                bid = pricing.Bid,
                ask = pricing.Ask,
                last = pricing.Last,
                mark = pricing.Mark,
                volatility = pricing.Volatility,
                delta = pricing.Delta,
                gamma = pricing.Gamma,
                theta = pricing.Theta,
                vega = pricing.Vega,
                rho = pricing.Rho,
                underlyingprice = FSharpOption<decimal>.get_IsNone(pricing.UnderlyingPrice)
                    ? null
                    : (decimal?)pricing.UnderlyingPrice.Value,
                timestamp = pricing.Timestamp
            });
        }

        public async Task<IEnumerable<StockPriceAlert>> GetStockPriceAlerts(UserId userId)
        {
            using var db = GetConnection();

            const string query = @"SELECT * FROM stockpricealerts WHERE userid = :userId ORDER BY createdat DESC";
            
            var reader = await db.ExecuteReaderAsync(query, new { userId = userId.Item });
            
            var result = new List<StockPriceAlert>();
            
            while (reader.Read())
            {
                var alert = new StockPriceAlert(
                    alertId: reader.GetGuid(reader.GetOrdinal("alertid")),
                    userId: UserId.NewUserId(reader.GetGuid(reader.GetOrdinal("userid"))),
                    ticker: new Ticker(reader.GetString(reader.GetOrdinal("ticker"))),
                    priceLevel: reader.GetDecimal(reader.GetOrdinal("pricelevel")),
                    alertType: PriceAlertTypeModule.fromString(reader.GetString(reader.GetOrdinal("alerttype"))),
                    note: reader.GetString(reader.GetOrdinal("note")),
                    state: PriceAlertStateModule.fromString(reader.GetString(reader.GetOrdinal("state"))),
                    createdAt: new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("createdat"))),
                    triggeredAt: reader.IsDBNull(reader.GetOrdinal("triggeredat")) 
                        ? FSharpOption<DateTimeOffset>.None 
                        : FSharpOption<DateTimeOffset>.Some(new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("triggeredat")))),
                    lastResetAt: reader.IsDBNull(reader.GetOrdinal("lastresetat")) 
                        ? FSharpOption<DateTimeOffset>.None 
                        : FSharpOption<DateTimeOffset>.Some(new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("lastresetat"))))
                );
                
                result.Add(alert);
            }

            return result;
        }

        public async Task SaveStockPriceAlert(StockPriceAlert alert)
        {
            using var db = GetConnection();

            var query = @"
INSERT INTO stockpricealerts (alertid, userid, ticker, pricelevel, alerttype, note, state, createdat, triggeredat, lastresetat)
VALUES (:alertid, :userid, :ticker, :pricelevel, :alerttype, :note, :state, :createdat, :triggeredat, :lastresetat)
ON CONFLICT (alertid) DO UPDATE SET
    pricelevel = EXCLUDED.pricelevel,
    alerttype = EXCLUDED.alerttype,
    note = EXCLUDED.note,
    state = EXCLUDED.state,
    triggeredat = EXCLUDED.triggeredat,
    lastresetat = EXCLUDED.lastresetat
            ";
            
            await db.ExecuteAsync(query, new
            {
                alertid = alert.AlertId,
                userid = alert.UserId.Item,
                ticker = alert.Ticker.Value,
                pricelevel = alert.PriceLevel,
                alerttype = PriceAlertTypeModule.toString(alert.AlertType),
                note = alert.Note,
                state = PriceAlertStateModule.toString(alert.State),
                createdat = alert.CreatedAt,
                triggeredat = FSharpOption<DateTimeOffset>.get_IsNone(alert.TriggeredAt) 
                    ? null 
                    : (DateTimeOffset?)alert.TriggeredAt.Value,
                lastresetat = FSharpOption<DateTimeOffset>.get_IsNone(alert.LastResetAt) 
                    ? null 
                    : (DateTimeOffset?)alert.LastResetAt.Value
            });
        }

        public async Task DeleteStockPriceAlert(Guid alertId)
        {
            using var db = GetConnection();

            const string query = @"DELETE FROM stockpricealerts WHERE alertid = :alertid";
            
            await db.ExecuteAsync(query, new { alertid = alertId });
        }

        public async Task<IEnumerable<Reminder>> GetReminders(UserId userId)
        {
            using var db = GetConnection();

            const string query = @"SELECT * FROM reminders WHERE userid = :userId ORDER BY date ASC";
            
            var reader = await db.ExecuteReaderAsync(query, new { userId = userId.Item });
            
            var result = new List<Reminder>();
            
            while (reader.Read())
            {
                var reminder = new Reminder(
                    reminderId: reader.GetGuid(reader.GetOrdinal("reminderid")),
                    userId: UserId.NewUserId(reader.GetGuid(reader.GetOrdinal("userid"))),
                    date: new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("date"))),
                    message: reader.GetString(reader.GetOrdinal("message")),
                    ticker: reader.IsDBNull(reader.GetOrdinal("ticker"))
                        ? FSharpOption<Ticker>.None
                        : FSharpOption<Ticker>.Some(new Ticker(reader.GetString(reader.GetOrdinal("ticker")))),
                    state: ReminderStateModule.fromString(reader.GetString(reader.GetOrdinal("state"))),
                    createdAt: new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("createdat"))),
                    sentAt: reader.IsDBNull(reader.GetOrdinal("sentat"))
                        ? FSharpOption<DateTimeOffset>.None
                        : FSharpOption<DateTimeOffset>.Some(new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("sentat"))))
                );
                
                result.Add(reminder);
            }

            return result;
        }

        public async Task SaveReminder(Reminder reminder)
        {
            using var db = GetConnection();

            var query = @"
INSERT INTO reminders (reminderid, userid, date, message, ticker, state, createdat, sentat)
VALUES (:reminderid, :userid, :date, :message, :ticker, :state, :createdat, :sentat)
ON CONFLICT (reminderid) DO UPDATE SET
    date = EXCLUDED.date,
    message = EXCLUDED.message,
    ticker = EXCLUDED.ticker,
    state = EXCLUDED.state,
    sentat = EXCLUDED.sentat
            ";
            
            await db.ExecuteAsync(query, new
            {
                reminderid = reminder.ReminderId,
                userid = reminder.UserId.Item,
                date = reminder.Date.ToUniversalTime(),
                message = reminder.Message,
                ticker = FSharpOption<Ticker>.get_IsNone(reminder.Ticker)
                    ? null
                    : reminder.Ticker.Value.Value,
                state = ReminderStateModule.toString(reminder.State),
                createdat = reminder.CreatedAt.ToUniversalTime(),
                sentat = FSharpOption<DateTimeOffset>.get_IsNone(reminder.SentAt)
                    ? null
                    : (DateTimeOffset?)reminder.SentAt.Value.ToUniversalTime()
            });
        }

        public async Task DeleteReminder(Guid reminderId)
        {
            using var db = GetConnection();

            const string query = @"DELETE FROM reminders WHERE reminderid = :reminderid";
            
            await db.ExecuteAsync(query, new { reminderid = reminderId });
        }
    }
}

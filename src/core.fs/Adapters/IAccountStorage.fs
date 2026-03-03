namespace core.fs.Adapters.Storage

open System
open System.Collections.Generic
open System.Threading.Tasks
open core.Shared
open core.fs.Accounts
open core.fs.Adapters.Brokerage
open core.fs.Options
open core.fs.Alerts
open core.fs.Stocks

[<CLIMutable>]
type TickerCikMapping = 
    {
        Ticker: string
        Cik: string
        Title: string
        LastUpdated: DateTimeOffset
    }

type IAccountStorage =
    
    abstract member GetUserByEmail: emailAddress:string -> Task<User option>
    abstract member GetUser: userId:UserId -> Task<User option>
    abstract member Save: u:User -> Task
    abstract member Delete: u:User -> Task
    abstract member SaveUserAssociation: r:ProcessIdToUserAssociation -> Task
    abstract member GetAccountBalancesSnapshots: start:DateTimeOffset -> ``end``:DateTimeOffset -> userId:UserId -> Task<AccountBalancesSnapshot seq>
    abstract member SaveAccountBalancesSnapshot: userId:UserId -> balances:AccountBalancesSnapshot -> Task
    abstract member GetAccountBrokerageOrders: userId:UserId -> Task<StockOrder seq>
    abstract member SaveAccountBrokerageStockOrders: userId:UserId -> orders:StockOrder seq -> Task
    abstract member SaveAccountBrokerageOptionOrders: userId:UserId -> orders:OptionOrder seq -> Task
    abstract member InsertAccountBrokerageTransactions: userId:UserId -> transactions:AccountTransaction seq -> Task
    abstract member SaveAccountBrokerageTransactions: userId:UserId -> transactions:AccountTransaction[] -> Task
    abstract member GetAccountBrokerageTransactions: userId:UserId -> Task<AccountTransaction seq>
    abstract member GetUserAssociation: guid:Guid -> Task<ProcessIdToUserAssociation option>
    abstract member GetUserEmailIdPairs: unit -> Task<IEnumerable<EmailIdPair>>
    abstract member GetOptionPricing : userId:UserId -> symbol:OptionTicker -> Task<OptionPricing seq>
    abstract member SaveOptionPricing : pricing:OptionPricing -> userId:UserId -> Task
    abstract member GetStockPriceAlerts: userId:UserId -> Task<StockPriceAlert seq>
    abstract member SaveStockPriceAlert: alert:StockPriceAlert -> Task
    abstract member DeleteStockPriceAlert: alertId:Guid -> userId:UserId -> Task
    abstract member GetReminders: userId:UserId -> Task<Reminder seq>
    abstract member SaveReminder: reminder:Reminder -> Task
    abstract member DeleteReminder: reminderId:Guid -> userId:UserId -> Task
    abstract member DeleteSentRemindersBefore: cutoffDate:DateTimeOffset -> Task<int>
    abstract member GetTickerCik: ticker:string -> Task<TickerCikMapping option>
    abstract member SaveTickerCikMappings: mappings:TickerCikMapping seq -> Task
    abstract member GetAllTickerCikMappings: unit -> Task<TickerCikMapping seq>
    abstract member GetTickerCikLastUpdated: unit -> Task<DateTimeOffset option>
    abstract member SearchTickerCik: query:string -> Task<TickerCikMapping seq>

type IStockListStorage =
    abstract member GetStockLists : userId:UserId -> Task<StockList seq>
    abstract member GetStockList : id:Guid -> userId:UserId -> Task<StockList option>
    abstract member SaveStockList : id:Guid option -> name:string -> description:string -> userId:UserId -> Task<StockList option>
    abstract member DeleteStockList : id:Guid -> userId:UserId -> Task
    abstract member DeleteAllStockLists : userId:UserId -> Task
    abstract member AddTickerToStockList : id:Guid -> ticker:Ticker -> note:string -> userId:UserId -> Task<StockList option>
    abstract member RemoveTickerFromStockList : id:Guid -> ticker:Ticker -> userId:UserId -> Task<StockList option>
    abstract member AddTagToStockList : id:Guid -> tag:string -> userId:UserId -> Task<StockList option>
    abstract member RemoveTagFromStockList : id:Guid -> tag:string -> userId:UserId -> Task<StockList option>
    abstract member ClearStockListTickers : id:Guid -> userId:UserId -> Task<StockList option>

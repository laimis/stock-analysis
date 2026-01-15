namespace core.fs.Adapters.Storage

open System
open System.Collections.Generic
open System.Threading.Tasks
open core.fs.Accounts
open core.fs.Adapters.Brokerage
open core.fs.Options
open core.fs.Alerts

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
    abstract member DeleteStockPriceAlert: alertId:Guid -> Task

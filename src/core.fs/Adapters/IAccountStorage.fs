namespace core.fs.Adapters.Storage

open System
open System.Collections.Generic
open System.Threading.Tasks
open core.fs.Accounts
open core.fs.Adapters.Brokerage

type IAccountStorage =
    
    abstract member GetUserByEmail: emailAddress:string -> Task<User option>
    abstract member GetUser: userId:UserId -> Task<User option>
    abstract member Save: u:User -> Task
    abstract member Delete: u:User -> Task
    abstract member SaveUserAssociation: r:ProcessIdToUserAssociation -> Task
    abstract member GetAccountBalancesSnapshots: start:DateTimeOffset -> ``end``:DateTimeOffset -> userId:UserId -> Task<AccountBalancesSnapshot seq>
    abstract member SaveAccountBalancesSnapshot: userId:UserId -> balances:AccountBalancesSnapshot -> Task
    abstract member GetAccountBrokerageOrders: userId:UserId -> Task<Order seq>
    abstract member SaveAccountBrokerageOrders: userId:UserId -> orders:Order seq -> Task
    abstract member GetUserAssociation: guid:Guid -> Task<ProcessIdToUserAssociation option>
    abstract member GetUserEmailIdPairs: unit -> Task<IEnumerable<EmailIdPair>>

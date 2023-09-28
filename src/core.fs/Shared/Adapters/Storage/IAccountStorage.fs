namespace core.fs.Shared.Adapters.Storage

open System
open System.Collections.Generic
open System.Threading.Tasks
open core.Account
open core.fs.Shared.Domain.Accounts

type IAccountStorage =
    
    abstract member GetUserByEmail: emailAddress:string -> Task<User>
    abstract member GetUser: userId:Guid -> Task<User>
    abstract member Save: u:User -> Task
    abstract member Delete: u:User -> Task
    abstract member SaveUserAssociation: r:ProcessIdToUserAssociation -> Task
    abstract member GetUserAssociation: guid:Guid -> Task<ProcessIdToUserAssociation option>
    abstract member GetUserEmailIdPairs: unit -> Task<IEnumerable<EmailIdPair>>
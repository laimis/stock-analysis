namespace core.fs.Shared.Adapters.Authentication

open core.Account

[<CLIMutable>]
type HashAndSalt = { Hash : string; Salt : string }

type IPasswordHashProvider =
    abstract member GenerateHashAndSalt : password:string -> saltLength:int -> HashAndSalt
    abstract member GenerateHash : password:string -> salt:string -> string

type IRoleService =
    abstract member IsAdmin : user:UserState -> bool
    abstract member GetAdminEmail : unit -> string
    
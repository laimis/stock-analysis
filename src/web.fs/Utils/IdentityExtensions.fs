namespace web.Utils

open System
open System.Security.Claims
open core.fs.Accounts

[<AutoOpen>]
module IdentityExtensions =
    
    [<Literal>]
    let ID_CLAIM_NAME = "userid"
    
    let private getClaimValue (p: ClaimsPrincipal) (name: string) =
        if isNull p then
            None
        else
            let claim = p.FindFirst(name)
            if isNull claim then
                None
            else
                Some claim.Value
    
    type ClaimsPrincipal with
        member this.Identifier() =
            match getClaimValue this ID_CLAIM_NAME with
            | Some guid -> UserId(Guid(guid))
            | None -> raise (Exception($"User is not authenticated. Missing claim: {ID_CLAIM_NAME}"))
        
        member this.Email() =
            match getClaimValue this ClaimTypes.Email with
            | Some email -> email
            | None -> null
        
        member this.Firstname() =
            match getClaimValue this ClaimTypes.GivenName with
            | Some name -> name
            | None -> null
        
        member this.Lastname() =
            match getClaimValue this ClaimTypes.Surname with
            | Some name -> name
            | None -> null

namespace di

open core.Account
open core.fs.Adapters.Authentication

type RoleService(adminEmail: string option) =
    
    interface IRoleService with
        member _.IsAdmin(user: UserState) = 
            match adminEmail with
            | Some email when not (System.String.IsNullOrWhiteSpace(email)) -> 
                user.Email = email
            | _ -> false
        
        member _.GetAdminEmail() = 
            match adminEmail with
            | Some email -> email
            | None -> null

namespace core.fs.Routines

open System
open System.ComponentModel.DataAnnotations
open core.Routines
open core.fs
open core.fs.Accounts
open core.fs.Adapters.Storage
open core.fs.Adapters.Storage

type Query =
    {
        UserId:UserId
    }
    
type Create =
    {
        Name:string
        Description:string
    }
    
type Update =
    {
        [<Required>]
        Id:Guid
        [<Required>]
        Name:string
        Description:string
    }
    
type Delete =
    {
        UserId:UserId
        Id:Guid
    }
    
type AddStep =
    {
        [<Required>]
        Id:Guid
        [<Required>]
        Label:string
        Url:string
    }
    
type MoveStep =
    {
        [<Required>]
        Id: Guid
        [<Required>]
        StepIndex:Nullable<int>
        [<Required>]
        Direction:Nullable<int>
    }
type RemoveStep =
    {
        [<Required>]
        Id: Guid
        [<Required>]
        StepIndex:Nullable<int>
        UserId:UserId
    }
    
type UpdateStep =
    {
        [<Required>]
        Id:Guid
        [<Required>]
        StepIndex:Nullable<int>
        [<Required>]
        Label:string
        Url:string
    }
    
type Handler(accounts:IAccountStorage,storage:IPortfolioStorage) =
    
    interface IApplicationService
    
    member _.HandleCreate userId (create:Create) = task {
        
        let! user = accounts.GetUser userId
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some _ ->
            let! routine = storage.GetRoutines userId
            let exists = routine |> Seq.exists (fun r -> r.State.Name.ToLower() = create.Name.ToLower())
            
            match exists with
            | false ->
                let routine = Routine(name=create.Name,description=create.Description,userId=(userId |> IdentifierHelper.getUserId))
                do! storage.SaveRoutine routine userId
                return routine.State |> Ok
            | true -> return "Routine already exists" |> ServiceError |> Error
    }
    
    member _.HandleUpdate userId (update:Update) = task {
        
        let! user = accounts.GetUser userId
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! routine = storage.GetRoutine update.Id userId
            match routine with
            | null -> return "Routine not found" |> ServiceError |> Error
            | _ ->
                routine.Update(name=update.Name,description=update.Description)
                do! storage.SaveRoutine routine userId
                return routine.State |> Ok
    }
    
    member _.Handle (delete:Delete) = task {
        
        let! user = accounts.GetUser delete.UserId
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! routine = storage.GetRoutine delete.Id delete.UserId
            match routine with
            | null -> return "Routine not found" |> ServiceError |> Error
            | _ ->
                do! storage.DeleteRoutine routine delete.UserId
                return routine.State |> Ok
    }
    
    member _.Handle (query:Query) = task {
        
        let! user = accounts.GetUser query.UserId
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! routines = storage.GetRoutines query.UserId
            return
                routines
                |> Seq.map (_.State)
                |> Seq.sortBy (_.Name.ToLower())
                |> Seq.toArray
                |> Ok
    }
    
    member _.HandleAddStep userId (add:AddStep) = task {
        
        let! user = accounts.GetUser userId
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! routine = storage.GetRoutine add.Id userId
            match routine with
            | null -> return "Routine not found" |> ServiceError |> Error
            | _ ->
                routine.AddStep(label=add.Label,url=add.Url)
                do! storage.SaveRoutine routine userId
                return routine.State |> Ok
    }
    
    member _.HandleMoveStep userId (move:MoveStep) = task {
        
        let! user = accounts.GetUser userId
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! routine = storage.GetRoutine move.Id userId
            match routine with
            | null -> return "Routine not found" |> ServiceError |> Error
            | _ ->
                routine.MoveStep(stepIndex=move.StepIndex.Value,direction=move.Direction.Value)
                do! storage.SaveRoutine routine userId
                return routine.State |> Ok
    }
    
    member _.Handle (remove:RemoveStep) = task {
        
        let! user = accounts.GetUser remove.UserId
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! routine = storage.GetRoutine remove.Id remove.UserId
            match routine with
            | null -> return "Routine not found" |> ServiceError |> Error
            | _ ->
                routine.RemoveStep(index=remove.StepIndex.Value)
                do! storage.SaveRoutine routine remove.UserId
                return routine.State |> Ok
    }
    
    member _.HandleUpdateStep userId (update:UpdateStep) = task {
        
        let! user = accounts.GetUser userId
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | _ ->
            let! routine = storage.GetRoutine update.Id userId
            match routine with
            | null -> return "Routine not found" |> ServiceError |> Error
            | _ ->
                routine.UpdateStep(index=update.StepIndex.Value,label=update.Label,url=update.Url)
                do! storage.SaveRoutine routine userId
                return routine.State |> Ok
    }
    
    
    

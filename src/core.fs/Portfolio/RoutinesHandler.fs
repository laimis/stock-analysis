namespace core.fs.Portfolio.Routines

open System
open System.ComponentModel.DataAnnotations
open core.Portfolio
open core.Shared
open core.fs.Shared
open core.fs.Shared.Adapters.Storage

type Query =
    {
        UserId:Guid
    }
    
type Create =
    {
        UserId:Guid
        Name:string
        Description:string
    }
    
    static member WithUserId (userId:Guid) (create:Create) =
        { create with UserId = userId }
    
type Update =
    {
        UserId:Guid
        [<Required>]
        Name:string
        [<Required>]
        NewName:string
        Description:string
    }
    
    static member WithUserId (userId:Guid) (update:Update) =
        { update with UserId = userId }
    
type Delete =
    {
        UserId:Guid
        Name:string
    }
    
type AddStep =
    {
        UserId:Guid
        [<Required>]
        RoutineName:string
        [<Required>]
        Label:string
        Url:string
    }
    static member WithUserId (userId:Guid) (add:AddStep) =
        { add with UserId = userId }

type MoveStep =
    {
        [<Required>]
        RoutineName: string
        [<Required>]
        StepIndex:Nullable<int>
        [<Required>]
        Direction:Nullable<int>
        UserId:Guid
    }
    static member WithUserId (userId:Guid) (move:MoveStep) =
        { move with UserId = userId }
    
type RemoveStep =
    {
        [<Required>]
        RoutineName: string
        [<Required>]
        StepIndex:Nullable<int>
        UserId:Guid
    }
    
type UpdateStep =
    {
        [<Required>]
        RoutineName:string
        [<Required>]
        StepIndex:Nullable<int>
        [<Required>]
        Label:string
        Url:string
        UserId:Guid
    }
    
    static member WithUserId (userId:Guid) (update:UpdateStep) =
        { update with UserId = userId }
    
type Handler(accounts:IAccountStorage,storage:IPortfolioStorage) =
    
    interface IApplicationService
    
    member _.Handle (create:Create) = task {
        
        let! user = accounts.GetUser create.UserId
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<RoutineState>
        | Some _ ->
            let! routine = storage.GetRoutine create.Name create.UserId
            match routine with
            | null ->
                let routine = Routine(name=create.Name,description=create.Description,userId=create.UserId)
                do! storage.SaveRoutine routine create.UserId
                return ServiceResponse<RoutineState>(routine.State)
            | _ -> return "Routine already exists" |> ResponseUtils.failedTyped<RoutineState>
    }
    
    member _.Handle (update:Update) = task {
        
        let! user = accounts.GetUser update.UserId
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<RoutineState>
        | _ ->
            let! routine = storage.GetRoutine update.Name update.UserId
            match routine with
            | null -> return "Routine not found" |> ResponseUtils.failedTyped<RoutineState>
            | _ ->
                routine.Update(name=update.NewName,description=update.Description)
                do! storage.SaveRoutine routine update.UserId
                return ServiceResponse<RoutineState>(routine.State)
    }
    
    member _.Handle (delete:Delete) = task {
        
        let! user = accounts.GetUser delete.UserId
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<RoutineState>
        | _ ->
            let! routine = storage.GetRoutine delete.Name delete.UserId
            match routine with
            | null -> return "Routine not found" |> ResponseUtils.failedTyped<RoutineState>
            | _ ->
                do! storage.DeleteRoutine routine delete.UserId
                return ServiceResponse<RoutineState>(routine.State)
    }
    
    member _.Handle (query:Query) = task {
        
        let! user = accounts.GetUser query.UserId
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<RoutineState array>
        | _ ->
            let! routines = storage.GetRoutines query.UserId
            let states =
                routines
                |> Seq.map (fun routine -> routine.State)
                |> Seq.sortBy (fun state -> state.Name.ToLower())
                |> Seq.toArray
            return ServiceResponse<RoutineState array>(states)
    }
    
    member _.Handle (add:AddStep) = task {
        
        let! user = accounts.GetUser add.UserId
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<RoutineState>
        | _ ->
            let! routine = storage.GetRoutine add.RoutineName add.UserId
            match routine with
            | null -> return "Routine not found" |> ResponseUtils.failedTyped<RoutineState>
            | _ ->
                routine.AddStep(label=add.Label,url=add.Url)
                do! storage.SaveRoutine routine add.UserId
                return ServiceResponse<RoutineState>(routine.State)
    }
    
    member _.Handle (move:MoveStep) = task {
        
        let! user = accounts.GetUser move.UserId
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<RoutineState>
        | _ ->
            let! routine = storage.GetRoutine move.RoutineName move.UserId
            match routine with
            | null -> return "Routine not found" |> ResponseUtils.failedTyped<RoutineState>
            | _ ->
                routine.MoveStep(stepIndex=move.StepIndex.Value,direction=move.Direction.Value)
                do! storage.SaveRoutine routine move.UserId
                return ServiceResponse<RoutineState>(routine.State)
    }
    
    member _.Handle (remove:RemoveStep) = task {
        
        let! user = accounts.GetUser remove.UserId
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<RoutineState>
        | _ ->
            let! routine = storage.GetRoutine remove.RoutineName remove.UserId
            match routine with
            | null -> return "Routine not found" |> ResponseUtils.failedTyped<RoutineState>
            | _ ->
                routine.RemoveStep(index=remove.StepIndex.Value)
                do! storage.SaveRoutine routine remove.UserId
                return ServiceResponse<RoutineState>(routine.State)
    }
    
    member _.Handle (update:UpdateStep) = task {
        
        let! user = accounts.GetUser update.UserId
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<RoutineState>
        | _ ->
            let! routine = storage.GetRoutine update.RoutineName update.UserId
            match routine with
            | null -> return "Routine not found" |> ResponseUtils.failedTyped<RoutineState>
            | _ ->
                routine.UpdateStep(index=update.StepIndex.Value,label=update.Label,url=update.Url)
                do! storage.SaveRoutine routine update.UserId
                return ServiceResponse<RoutineState>(routine.State)
    }
    
    
    
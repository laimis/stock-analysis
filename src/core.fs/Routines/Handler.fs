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
        Name:string
        [<Required>]
        NewName:string
        Description:string
    }
    
type Delete =
    {
        UserId:UserId
        Name:string
    }
    
type AddStep =
    {
        [<Required>]
        RoutineName:string
        [<Required>]
        Label:string
        Url:string
    }
    
type MoveStep =
    {
        [<Required>]
        RoutineName: string
        [<Required>]
        StepIndex:Nullable<int>
        [<Required>]
        Direction:Nullable<int>
    }
type RemoveStep =
    {
        [<Required>]
        RoutineName: string
        [<Required>]
        StepIndex:Nullable<int>
        UserId:UserId
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
    }
    
type Handler(accounts:IAccountStorage,storage:IPortfolioStorage) =
    
    interface IApplicationService
    
    member _.HandleCreate userId (create:Create) = task {
        
        let! user = accounts.GetUser userId
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<RoutineState>
        | Some _ ->
            let! routine = storage.GetRoutine create.Name userId
            match routine with
            | null ->
                let routine = Routine(name=create.Name,description=create.Description,userId=(userId |> IdentifierHelper.getUserId))
                do! storage.SaveRoutine routine userId
                return ServiceResponse<RoutineState>(routine.State)
            | _ -> return "Routine already exists" |> ResponseUtils.failedTyped<RoutineState>
    }
    
    member _.HandleUpdate userId (update:Update) = task {
        
        let! user = accounts.GetUser userId
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<RoutineState>
        | _ ->
            let! routine = storage.GetRoutine update.Name userId
            match routine with
            | null -> return "Routine not found" |> ResponseUtils.failedTyped<RoutineState>
            | _ ->
                routine.Update(name=update.NewName,description=update.Description)
                do! storage.SaveRoutine routine userId
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
    
    member _.HandleAddStep userId (add:AddStep) = task {
        
        let! user = accounts.GetUser userId
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<RoutineState>
        | _ ->
            let! routine = storage.GetRoutine add.RoutineName userId
            match routine with
            | null -> return "Routine not found" |> ResponseUtils.failedTyped<RoutineState>
            | _ ->
                routine.AddStep(label=add.Label,url=add.Url)
                do! storage.SaveRoutine routine userId
                return ServiceResponse<RoutineState>(routine.State)
    }
    
    member _.HandleMoveStep userId (move:MoveStep) = task {
        
        let! user = accounts.GetUser userId
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<RoutineState>
        | _ ->
            let! routine = storage.GetRoutine move.RoutineName userId
            match routine with
            | null -> return "Routine not found" |> ResponseUtils.failedTyped<RoutineState>
            | _ ->
                routine.MoveStep(stepIndex=move.StepIndex.Value,direction=move.Direction.Value)
                do! storage.SaveRoutine routine userId
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
    
    member _.HandleUpdateStep userId (update:UpdateStep) = task {
        
        let! user = accounts.GetUser userId
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<RoutineState>
        | _ ->
            let! routine = storage.GetRoutine update.RoutineName userId
            match routine with
            | null -> return "Routine not found" |> ResponseUtils.failedTyped<RoutineState>
            | _ ->
                routine.UpdateStep(index=update.StepIndex.Value,label=update.Label,url=update.Url)
                do! storage.SaveRoutine routine userId
                return ServiceResponse<RoutineState>(routine.State)
    }
    
    
    
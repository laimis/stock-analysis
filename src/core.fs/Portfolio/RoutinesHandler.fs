namespace core.fs.Portfolio

open System
open System.ComponentModel.DataAnnotations
open core.Portfolio
open core.Shared
open core.Shared.Adapters.Storage
open core.fs

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
    
type Update =
    {
        UserId:Guid
        [<Required>]
        Name:string
        [<Required>]
        NewName:string
        Description:string
    }
    
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
    
type Routines(accounts:IAccountStorage,storage:IPortfolioStorage) =
    
    interface IApplicationService
    
    member _.Handle (create:Create) = task {
        
        let! user = accounts.GetUser create.UserId
        match user with
        | null -> return "User not found" |> ResponseUtils.failedTyped<RoutineState>
        | _ ->
            let! routine = storage.GetRoutine(name=create.Name, userId=create.UserId)
            match routine with
            | null ->
                let routine = Routine(name=create.Name,description=create.Description,userId=create.UserId)
                do! storage.Save(routine, userId=create.UserId)
                return ServiceResponse<RoutineState>(routine.State)
            | _ -> return "Routine already exists" |> ResponseUtils.failedTyped<RoutineState>
    }
    
    member _.Handle (update:Update) = task {
        
        let! user = accounts.GetUser update.UserId
        match user with
        | null -> return "User not found" |> ResponseUtils.failedTyped<RoutineState>
        | _ ->
            let! routine = storage.GetRoutine(name=update.Name, userId=update.UserId)
            match routine with
            | null -> return "Routine not found" |> ResponseUtils.failedTyped<RoutineState>
            | _ ->
                routine.Update(name=update.NewName,description=update.Description)
                do! storage.Save(routine, userId=update.UserId)
                return ServiceResponse<RoutineState>(routine.State)
    }
    
    member _.Handle (delete:Delete) = task {
        
        let! user = accounts.GetUser delete.UserId
        match user with
        | null -> return "User not found" |> ResponseUtils.failedTyped<RoutineState>
        | _ ->
            let! routine = storage.GetRoutine(name=delete.Name, userId=delete.UserId)
            match routine with
            | null -> return "Routine not found" |> ResponseUtils.failedTyped<RoutineState>
            | _ ->
                do! storage.DeleteRoutine(routine, userId=delete.UserId)
                return ServiceResponse<RoutineState>(routine.State)
    }
    
    member _.Handle (query:Query) = task {
        
        let! user = accounts.GetUser query.UserId
        match user with
        | null -> return "User not found" |> ResponseUtils.failedTyped<RoutineState array>
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
        | null -> return "User not found" |> ResponseUtils.failedTyped<RoutineState>
        | _ ->
            let! routine = storage.GetRoutine(name=add.RoutineName,userId=add.UserId)
            match routine with
            | null -> return "Routine not found" |> ResponseUtils.failedTyped<RoutineState>
            | _ ->
                routine.AddStep(label=add.Label,url=add.Url)
                do! storage.Save(routine, userId=add.UserId)
                return ServiceResponse<RoutineState>(routine.State)
    }
    
    member _.Handle (move:MoveStep) = task {
        
        let! user = accounts.GetUser move.UserId
        match user with
        | null -> return "User not found" |> ResponseUtils.failedTyped<RoutineState>
        | _ ->
            let! routine = storage.GetRoutine(name=move.RoutineName,userId=move.UserId)
            match routine with
            | null -> return "Routine not found" |> ResponseUtils.failedTyped<RoutineState>
            | _ ->
                routine.MoveStep(stepIndex=move.StepIndex.Value,direction=move.Direction.Value)
                do! storage.Save(routine, userId=move.UserId)
                return ServiceResponse<RoutineState>(routine.State)
    }
    
    member _.Handle (remove:RemoveStep) = task {
        
        let! user = accounts.GetUser remove.UserId
        match user with
        | null -> return "User not found" |> ResponseUtils.failedTyped<RoutineState>
        | _ ->
            let! routine = storage.GetRoutine(name=remove.RoutineName,userId=remove.UserId)
            match routine with
            | null -> return "Routine not found" |> ResponseUtils.failedTyped<RoutineState>
            | _ ->
                routine.RemoveStep(index=remove.StepIndex.Value)
                do! storage.Save(routine, userId=remove.UserId)
                return ServiceResponse<RoutineState>(routine.State)
    }
    
    member _.Handle (update:UpdateStep) = task {
        
        let! user = accounts.GetUser update.UserId
        match user with
        | null -> return "User not found" |> ResponseUtils.failedTyped<RoutineState>
        | _ ->
            let! routine = storage.GetRoutine(name=update.RoutineName,userId=update.UserId)
            match routine with
            | null -> return "Routine not found" |> ResponseUtils.failedTyped<RoutineState>
            | _ ->
                routine.UpdateStep(index=update.StepIndex.Value,label=update.Label,url=update.Url)
                do! storage.Save(routine, userId=update.UserId)
                return ServiceResponse<RoutineState>(routine.State)
    }
    
    
    
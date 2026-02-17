#nullable enable
namespace core.Routines

open System
open System.Collections.Generic
open core.Shared

[<Struct>]
type RoutineStep =
    { label: string
      url: string option }

type RoutineState() =
    let mutable _id = Guid.Empty
    let mutable _userId = Guid.Empty
    let mutable _name: string = null
    let mutable _description: string option = None
    let _steps = List<RoutineStep>()

    member _.Id = _id
    member _.UserId = _userId
    member _.Name = _name
    member _.Description = _description
    member _.Steps = _steps :> IList<_>

    member this.Apply(e: AggregateEvent) =
        this.ApplyInternal(e :> obj)

    member private this.ApplyInternal(obj: obj) =
        match obj with
        | :? RoutineCreated as e -> this.ApplyInternal(e)
        | :? RoutineUpdated as e -> this.ApplyInternal(e)
        | :? RoutineStepAdded as e -> this.ApplyInternal(e)
        | :? RoutineStepRemoved as e -> this.ApplyInternal(e)
        | :? RoutineStepUpdated as e -> this.ApplyInternal(e)
        | :? RoutineStepMoved as e -> this.ApplyInternal(e)
        | _ -> ()

    member private this.ApplyInternal(e: RoutineCreated) =
        _id <- e.AggregateId
        _userId <- e.UserId
        _name <- e.Name
        _description <- if String.IsNullOrWhiteSpace(e.Description) then None else Some e.Description

    member private this.ApplyInternal(e: RoutineUpdated) =
        _name <- e.Name
        _description <- if String.IsNullOrWhiteSpace(e.Description) then None else Some e.Description

    member private this.ApplyInternal(e: RoutineStepAdded) =
        _steps.Add({ label = e.Label; url = if String.IsNullOrWhiteSpace(e.Url) then None else Some e.Url })

    member private this.ApplyInternal(e: RoutineStepRemoved) =
        _steps.RemoveAt(e.Index)

    member private this.ApplyInternal(e: RoutineStepUpdated) =
        _steps.[e.Index] <- { label = e.Label; url = if String.IsNullOrWhiteSpace(e.Url) then None else Some e.Url }

    member private this.ApplyInternal(e: RoutineStepMoved) =
        let step = _steps.[e.StepIndex]
        _steps.RemoveAt(e.StepIndex)
        _steps.Insert(e.StepIndex + e.Direction, step)

    interface IAggregateState with
        member this.Id = this.Id
        member this.Apply(e) = this.Apply(e)

type Routine =
    inherit Aggregate<RoutineState>

    new (events: IEnumerable<AggregateEvent>) = { inherit Aggregate<RoutineState>(events) }

    new (description: string, name: string, userId: Guid) as this =
        { inherit Aggregate<RoutineState>() }
        then
            if userId = Guid.Empty then
                raise (InvalidOperationException("Missing user id"))

            if String.IsNullOrWhiteSpace(name) then
                raise (InvalidOperationException("Missing routine name"))

            this.Apply(RoutineCreated(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, description, name, userId))

    member this.Update(name: string, ?description: string) =
        if String.IsNullOrWhiteSpace(name) then
            raise (InvalidOperationException("Missing routine name"))

        let name = name.Trim()
        let description = description |> Option.map (fun d -> d.Trim())

        if name = this.State.Name && description = this.State.Description then
            ()
        else
            this.Apply(RoutineUpdated(Guid.NewGuid(), this.State.Id, DateTimeOffset.UtcNow, defaultArg description null, name))

    member this.AddStep(label: string, ?url: string) =
        if String.IsNullOrWhiteSpace(label) then
            raise (InvalidOperationException("Missing step label"))

        let label = label.Trim()
        let url = url |> Option.map (fun u -> u.Trim())

        this.Apply(RoutineStepAdded(Guid.NewGuid(), this.State.Id, DateTimeOffset.UtcNow, label, defaultArg url null))

    member this.UpdateStep(index: int, label: string, ?url: string) =
        if index < 0 || index >= this.State.Steps.Count then
            raise (InvalidOperationException("Invalid step index"))

        if String.IsNullOrWhiteSpace(label) then
            raise (InvalidOperationException("Missing step label"))

        let label = label.Trim()
        let url = url |> Option.map (fun u -> u.Trim())

        let step = this.State.Steps.[index]
        if label = step.label && url = step.url then
            ()
        else
            this.Apply(RoutineStepUpdated(Guid.NewGuid(), this.State.Id, DateTimeOffset.UtcNow, index, label, defaultArg url null))

    member this.MoveStep(stepIndex: int, direction: int) =
        if stepIndex < 0 || stepIndex >= this.State.Steps.Count then
            raise (InvalidOperationException("Invalid step index"))

        if direction <> 1 && direction <> -1 then
            raise (InvalidOperationException("Invalid direction"))

        if stepIndex = 0 && direction = -1 then
            ()
        elif stepIndex = this.State.Steps.Count - 1 && direction = 1 then
            ()
        else
            this.Apply(RoutineStepMoved(Guid.NewGuid(), this.State.Id, DateTimeOffset.UtcNow, direction = direction, stepIndex = stepIndex))

    member this.RemoveStep(index: int) =
        if index < 0 || index >= this.State.Steps.Count then
            raise (InvalidOperationException("Invalid step index"))

        this.Apply(RoutineStepRemoved(Guid.NewGuid(), this.State.Id, DateTimeOffset.UtcNow, index))

#nullable restore

namespace web.Controllers

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Authorization
open core.fs.Routines
open web.Utils

[<ApiController>]
[<Authorize>]
[<Route("api/[controller]")>]
type RoutinesController(handler: Handler) =
    inherit ControllerBase()

    [<HttpGet>]
    member this.GetRoutines() =
        this.OkOrError(handler.Handle({Query.UserId = this.User.Identifier()}))

    [<HttpPost>]
    member this.CreateRoutine([<FromBody>] command: Create) =
        this.OkOrError(handler.HandleCreate (this.User.Identifier()) command)

    [<HttpPut("{id}")>]
    member this.UpdateRoutine([<FromBody>] command: Update) =
        this.OkOrError(handler.HandleUpdate (this.User.Identifier()) command)

    [<HttpDelete("{id}")>]
    member this.DeleteRoutine([<FromRoute>] id: Guid) =
        this.OkOrError(handler.Handle({Delete.UserId = this.User.Identifier(); Id = id}))

    [<HttpPut("{id}/steps")>]
    member this.AddRoutineStep([<FromBody>] command: AddStep) =
        this.OkOrError(handler.HandleAddStep (this.User.Identifier()) command)

    [<HttpPost("{id}/steps/{stepIndex}")>]
    member this.UpdateRoutineStep([<FromBody>] command: UpdateStep) =
        this.OkOrError(handler.HandleUpdateStep (this.User.Identifier()) command)

    [<HttpDelete("{id}/steps/{stepIndex}")>]
    member this.RemoveRoutineStep([<FromRoute>] id: Guid, [<FromRoute>] stepIndex: int) =
        this.OkOrError(handler.Handle({RemoveStep.Id = id; StepIndex = Nullable(stepIndex); UserId = this.User.Identifier()}))

    [<HttpPost("{id}/steps/{stepIndex}/position")>]
    member this.MoveRoutineStep([<FromBody>] cmd: MoveStep) =
        this.OkOrError(handler.HandleMoveStep (this.User.Identifier()) cmd)

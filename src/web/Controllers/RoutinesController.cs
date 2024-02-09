using System.Threading.Tasks;
using core.fs.Routines;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

namespace web.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class RoutinesController(Handler handler) : ControllerBase
{
    [HttpGet]
    public Task<ActionResult> GetRoutines() =>
        this.OkOrError(handler.Handle(new Query(User.Identifier())));

    [HttpPost]
    public Task<ActionResult> CreateRoutine([FromBody] Create command) =>
        this.OkOrError(
            handler.HandleCreate(
                User.Identifier(),
                command
            )
        );

    [HttpPut("{routineName}")]
    public Task UpdateRoutine([FromBody]Update command) =>
        this.OkOrError(
            handler.HandleUpdate(
                User.Identifier(),
                command
            )
        );

    [HttpDelete("{routineName}")]
    public Task DeleteRoutine([FromRoute]string routineName) =>
        this.OkOrError(
            handler.Handle(
                new Delete(User.Identifier(), routineName)
            )
        );

    [HttpPut("{routineName}/steps")]
    public Task<ActionResult> AddRoutineStep([FromBody]AddStep command) =>
        this.OkOrError(
            handler.HandleAddStep(
                User.Identifier(), command
            )
        );
    
    [HttpPost("{routineName}/steps/{stepIndex}")]
    public Task<ActionResult> UpdateRoutineStep([FromBody]UpdateStep command) =>
        this.OkOrError(
            handler.HandleUpdateStep(
                User.Identifier(), command
            )
        );


    [HttpDelete("{routineName}/steps/{stepIndex}")]
    public Task<ActionResult> RemoveRoutineStep([FromRoute] string routineName, [FromRoute] int stepIndex) =>
        this.OkOrError(
            handler.Handle(
                new RemoveStep(routineName, stepIndex, User.Identifier())
            )
        );

    [HttpPost("{routineName}/steps/{stepIndex}/position")]
    public Task<ActionResult> MoveRoutineStep([FromBody] MoveStep cmd) =>
        this.OkOrError(
            handler.HandleMoveStep(
                User.Identifier(), cmd
            )
        );
}

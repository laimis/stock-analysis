using System.Threading.Tasks;
using core;
using core.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace web.Utils
{
    public static class ControllerBaseExtensions
    {
        public static async Task<ActionResult> GenerateExport(
            this ControllerBase controller,
            IMediator mediator,
            IRequest<ExportResponse> query)
        {
            var response = await mediator.Send(query);

            return controller.GenerateExport(response);
        }

        public static ActionResult GenerateExport(
            this ControllerBase controller,
            ExportResponse response)
            {
                controller.HttpContext.Response.Headers.Add(
                    "content-disposition", 
                    $"attachment; filename={response.Filename}");

                return new ContentResult
                {
                    Content = response.Content,
                    ContentType = response.ContentType
                };
            }

        public static ActionResult Error(
            this ControllerBase controller,
            string error)
        {
            
            var dict = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();
            dict.AddModelError("error", error);
            return controller.BadRequest(dict);
        }

        public static ActionResult OkOrError(
            this ControllerBase controller,
            CommandResponse r)
        {
            if (r.Error != null)
            {
                return controller.Error(r.Error);
            }

            return controller.Ok();
        }

        public static ActionResult OkOrError<T>(
            this ControllerBase controller,
            CommandResponse<T> r)
        {
            return r.Error switch {
                null => controller.Ok(r.Aggregate),
                _ => controller.Error(r.Error)
            };
        }

        public static async Task<ActionResult> ExecuteAsync<T>(this ControllerBase controller, IMediator mediator, IRequest<CommandResponse<T>> request)
        {
            var result = await mediator.Send(request);
            return controller.OkOrError(result);
        }
    }
}
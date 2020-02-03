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

            controller.HttpContext.Response.Headers.Add(
                "content-disposition", 
                $"attachment; filename={response.Filename}");

            return new ContentResult
            {
                Content = response.Content,
                ContentType = response.ContentType
            };
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

        public static ActionResult Error(
            this ControllerBase controller,
            string error)
        {
            
            var dict = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();
            dict.AddModelError("error", error);
            return controller.BadRequest(dict);
        }
    }
}
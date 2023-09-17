using System.Threading.Tasks;
using core.fs.Options;
using core.Shared;
using core.Shared.Adapters.CSV;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace web.Utils
{
    public static class ControllerBaseExtensions
    {
        public static Task<ActionResult> GenerateExport(
            this ControllerBase controller,
            IMediator mediator,
            IRequest<ServiceResponse<ExportResponse>> query)
        {
            return controller.GenerateExport(mediator.Send(query));
        }

        public static async Task<ActionResult> GenerateExport(
            this ControllerBase controller,
            Task<ServiceResponse<ExportResponse>> responseTask)
            {
                var response = await responseTask;

                if (response.IsOk)
                {
                    var content = response.Success;
                    controller.HttpContext.Response.Headers.Add(
                        "content-disposition", 
                        $"attachment; filename={content.Filename}");

                    return new ContentResult
                    {
                        Content = content.Content,
                        ContentType = content.ContentType
                    };
                }

                return controller.Error(response.Error!.Message);
            }

        public static ActionResult Error(
            this ControllerBase controller,
            string error)
        {
            
            var dict = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();
            dict.AddModelError("error", error);
            return controller.BadRequest(dict);
        }

        public static async Task<ActionResult> OkOrError<T>(
            this ControllerBase controller,
            Task<CommandResponse<T>> r)
        {
            return controller.OkOrError(await r);
        }
        
        public static async Task<ActionResult> OkOrError(
            this ControllerBase controller,
            Task<ServiceResponse> r)
        {
            return controller.OkOrError(await r);
        }
        
        public static async Task<ActionResult> OkOrError<T>(
            this ControllerBase controller,
            Task<ServiceResponse<T>> r)
        {
            return controller.OkOrError(await r);
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
        
        public static ActionResult OkOrError(
            this ControllerBase controller,
            ServiceResponse r)
        {
            if (r.Error != null)
            {
                return controller.Error(r.Error.Message);
            }

            return controller.Ok();
        }
        
        public static ActionResult OkOrError<T>(
            this ControllerBase controller,
            ServiceResponse<T> r)
        {
            if (r.IsOk == false)
            {
                return controller.Error(r.Error!.Message);
            }

            return controller.Ok(r.Success);
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
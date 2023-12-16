using System.Threading.Tasks;
using core.fs;
using core.fs.Adapters.CSV;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace web.Utils
{
    public static class ControllerBaseExtensions
    {
        public static async Task<ActionResult> GenerateExport(
            this ControllerBase controller,
            Task<ServiceResponse<ExportResponse>> responseTask)
            {
                var response = await responseTask;

                if (response.IsOk)
                {
                    var content = response.Success.Value;
                    controller.HttpContext.Response.Headers["Content-Disposition"] = new StringValues($"attachment; filename={content.Filename}");

                    return new ContentResult
                    {
                        Content = content.Content,
                        ContentType = content.ContentType
                    };
                }

                return controller.Error(response.Error.Value.Message);
            }

        public static ActionResult Error(
            this ControllerBase controller,
            string error)
        {
            
            var dict = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();
            dict.AddModelError("error", error);
            return controller.BadRequest(dict);
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
            ServiceResponse r)
        {
            if (r.IsError)
            {
                var error = r as ServiceResponse.Error;
                return controller.Error(error!.Item.Message);
            }

            return controller.Ok();
        }
        
        public static ActionResult OkOrError<T>(
            this ControllerBase controller,
            ServiceResponse<T> r)
        {
            if (r.IsOk == false)
            {
                return controller.Error(r.Error.Value.Message);
            }

            return controller.Ok(r.Success);
        }
    }
}
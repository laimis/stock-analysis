using System.Threading.Tasks;
using core.fs;
using core.fs.Adapters.CSV;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.FSharp.Core;

namespace web.Utils
{
    public static class ControllerBaseExtensions
    {
        public static ActionResult GenerateExport(
            this ControllerBase controller,
            ExportResponse response)
        {
            controller.HttpContext.Response.Headers.ContentDisposition = new StringValues($"attachment; filename={response.Filename}");

            return new ContentResult
            {
                Content = response.Content,
                ContentType = response.ContentType
            };
        }
        public static async Task<ActionResult> GenerateExport(
            this ControllerBase controller,
            Task<FSharpResult<ExportResponse,ServiceError>> responseTask)
            {
                var response = await responseTask;

                if (response.IsError)
                {
                    return controller.Error(response.ErrorValue.Message);
                }

                return controller.GenerateExport(response.ResultValue);
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
            Task<FSharpResult<Unit,ServiceError>> r)
        {
            return controller.OkOrError(await r);
        }
        
        public static async Task<ActionResult> OkOrError<T>(
            this ControllerBase controller,
            Task<FSharpResult<T,ServiceError>> r)
        {
            return controller.OkOrError(await r);
        }
        
        public static ActionResult OkOrError(
            this ControllerBase controller,
            FSharpResult<Unit,ServiceError> r)
        {
            if (r.IsError)
            {
                return controller.Error(r.ErrorValue.Message);
            }

            return controller.Ok();
        }
        
        public static ActionResult OkOrError<T>(
            this ControllerBase controller,
            FSharpResult<T,ServiceError> r)
        {
            if (r.IsOk == false)
            {
                return controller.Error(r.ErrorValue.Message);
            }

            return controller.Ok(r.ResultValue);
        }
    }
}

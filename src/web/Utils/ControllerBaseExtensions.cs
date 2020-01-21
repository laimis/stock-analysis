using System.Security.Claims;
using System.Threading.Tasks;
using core;
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
    }
}
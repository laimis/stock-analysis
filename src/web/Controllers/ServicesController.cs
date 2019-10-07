using System;
using System.Security.Claims;
using core.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using web.Utils;

namespace web.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class ServicesController : Controller
    {
        private IConfiguration _config;
        private IOptionsService _options;

        public ServicesController(
            IConfiguration config,
            IOptionsService service)
        {
            this._config = config;
            this._options = service;
        }

        [HttpGet]
        public async System.Threading.Tasks.Task<object> OptionsInfoAsync()
        {
            var r = await this._options.ServiceInformation();

            return r;
        }

        [HttpGet("identity")]
        public ActionResult Identity()
        {
            foreach(var c in this.User.Claims)
            {
                Console.WriteLine(c.Type + "-" + c.Value);
            }

            return new ContentResult {
                Content = this.User.Identifier(),
                ContentType = "application/json"
            };
        }
    }
}
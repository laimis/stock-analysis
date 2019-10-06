using core.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace web.Controllers
{
    [Route("api/[controller]")]
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
        public object OptionsInfo()
        {
            return this._options.ServiceInformation();
        }
    }
}
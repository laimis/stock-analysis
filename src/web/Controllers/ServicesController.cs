using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace web.Controllers
{
    [Route("api/[controller]")]
    public class ServicesController : Controller
    {
        private IConfiguration _config;

        public ServicesController(IConfiguration config)
        {
            this._config = config;
        }

        [HttpGet]
        public ActionResult Test()
        {
            return Json(this._config.AsEnumerable());
        }
    }
}
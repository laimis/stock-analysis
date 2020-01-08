using core.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

namespace web.Controllers
{
    [Route("api/[controller]")]
    [Authorize("admin")]
    public class AdminController : Controller
    {
        private IAccountStorage _storage;

        public AdminController(IAccountStorage storage)
        {
            _storage = storage;
        }
        
        [HttpGet("users")]
        public async System.Threading.Tasks.Task<object> UsersAsync()
        {
            var list = await this._storage.GetLogins();

            return list;
        }
    }
}
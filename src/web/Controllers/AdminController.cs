using System.Threading.Tasks;
using core.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace web.Controllers
{
    [ApiController]
    [Authorize("admin")]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private IAccountStorage _storage;

        public AdminController(IAccountStorage storage)
        {
            _storage = storage;
        }
        
        [HttpGet("users")]
        public async Task<object> UsersAsync()
        {
            var list = await this._storage.GetLogins();

            return list;
        }
    }
}
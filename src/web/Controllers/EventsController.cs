using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using storage.redis;
using storage.shared;
using web.Utils;

namespace web.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private IAggregateStorage _storage;
        private Migration _migration;

        public EventsController(IAggregateStorage storage, Migration migration)
        {
            _storage = storage;
            _migration = migration;
        }

        [HttpGet]
        public async Task<object> Index()
        {
            return await this._storage.GetStoredEvents("ownedstock", this.User.Identifier());
        }

        [HttpGet("fix")]
        public async Task<int> Fix()
        {
            return await _migration.FixMistakenEntry();
        }
    }
}
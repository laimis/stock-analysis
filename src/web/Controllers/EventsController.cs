using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using storage.redis;
using storage.shared;
using web.Utils;

namespace web.Controllers
{
    [ApiController]
    [Authorize("admin")]
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
        public async Task<object> Index(string entity)
        {
            return await this._storage.GetStoredEvents(entity, this.User.Identifier());
        }
    }
}
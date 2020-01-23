using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public EventsController(IAggregateStorage storage)
        {
            _storage = storage;
        }

        [HttpGet]
        public async Task<object> Index()
        {
            return await this._storage.GetStoredEvents("ownedstock", this.User.Identifier());
        }

        [HttpGet("fix")]
        public async Task Fix()
        {
            var redis = (storage.redis.AggregateStorage)_storage;

            await redis.FixEvents("ownedstock", this.User.Identifier());
            
            await redis.FixEvents("soldoption", this.User.Identifier());
        }
    }
}
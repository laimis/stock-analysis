using System.Threading.Tasks;
using core.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using storage.postgres;

namespace web.Controllers
{
    [Route("api/[controller]")]
    [Authorize("admin")]
    public class AdminController : ControllerBase
    {
        private IAccountStorage _storage;
        private storage.postgres.AggregateStorage _postgres;
        private storage.redis.AggregateStorage _redis;

        public AdminController(
            IAccountStorage storage,
            storage.postgres.AggregateStorage postgres,
            storage.redis.AggregateStorage redis)
        {
            _storage = storage;
            _postgres = postgres;
            _redis = redis;
        }
        
        [HttpGet("users")]
        public async Task<object> UsersAsync()
        {
            var list = await this._storage.GetLogins();

            return list;
        }

        [HttpGet("migrate")]
        public async Task MigrateAsync()
        {
            var events = await _postgres.GetEventsForMigration();

            await _redis.StoreEventsMigration(events);
        }
    }
}
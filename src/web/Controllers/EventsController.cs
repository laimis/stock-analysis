using System;
using System.Linq;
using System.Threading.Tasks;
using core.fs.Shared.Domain.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        
        public EventsController(IAggregateStorage storage)
        {
            _storage = storage;
        }

        [HttpGet]
        public async Task<object> Index(string entity, Guid? userId, Guid? aggregateId)
        {
            var u = userId == null ? null : UserId.NewUserId(userId.Value);
            
            var list = await _storage.GetStoredEvents(
                entity,
                u);

            if (aggregateId != null)
            {
                list = list
                    .Where(e => e.Event.AggregateId == aggregateId.Value)
                    .OrderBy(e => e.Version);
            }

            return list;
        }
    }
}
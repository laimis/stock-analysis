using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core;
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
        public async Task<object> Index(string entity, Guid? userId, Guid? aggregateId)
        {
            var list = await _storage.GetStoredEvents(
                entity,
                userId ?? User.Identifier());

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
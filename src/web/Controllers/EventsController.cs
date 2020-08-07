using System;
using System.Collections.Generic;
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
        public async Task<object> Index(string entity)
        {
            var list = await this._storage.GetStoredEvents(
                entity,
                this.User.Identifier());

            // return list;

            var filtered = new List<IEnumerable<object>>();
            foreach(var s in list)
            {
                if (s.EventJson.Contains("IRBT"))
                {
                    Console.WriteLine("irbt");
                    filtered.Add(new object[] {s.Created, s.Key, s.Version, s.EventJson });
                }
            }

            var body = CSVExport.GenerateRaw(
                "created,key,version,json",
                filtered);

            var response = new ExportResponse("events.csv", body);

            HttpContext.Response.Headers.Add(
                "content-disposition", 
                $"attachment; filename={response.Filename}");

            Console.WriteLine(response.Content);

            return new ContentResult
            {
                Content = response.Content,
                ContentType = response.ContentType
            };
        }
    }
}
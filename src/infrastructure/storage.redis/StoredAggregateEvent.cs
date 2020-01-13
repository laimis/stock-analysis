using System;
using core.Shared;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace storage.redis
{
    internal class StoredAggregateEvent
    {
        static JsonSerializerSettings _formatting = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects
        };

        public string Entity { get; set; }
        public string UserId { get; set; }
        public string Key { get; set; }
        public DateTime Created { get; set; }
        public int Version { get; set; }
        public AggregateEvent Event { get; set; }
        public static AggregateEvent GetEvent(string json)
        {
            return JsonConvert.DeserializeObject<AggregateEvent>(json, _formatting);
        }

        internal static string ToJson(AggregateEvent e)
        {
            return JsonConvert.SerializeObject(e, _formatting);
        }
    }
}
using System;
using core.Shared;
using Newtonsoft.Json;

namespace storage.shared
{
    public class StoredAggregateEvent
    {
        static JsonSerializerSettings _formatting = new()
        {
            TypeNameHandling = TypeNameHandling.Objects
        };

        public string Entity { get; set; }
        public Guid UserId { get; set; }
        
        public string AggregateId { get; set; }
        
        public string Key { get; set; }
        public DateTimeOffset Created { get; set; }

        public string EventJson
        {
            get => JsonConvert.SerializeObject(Event, _formatting);

            set => Event = JsonConvert.DeserializeObject<AggregateEvent>(
                EventInfraAdjustments.AdjustIfNeeded(value),
                _formatting);
        }

        public int Version { get; set; }
        public AggregateEvent Event { get; set; }
    }
}
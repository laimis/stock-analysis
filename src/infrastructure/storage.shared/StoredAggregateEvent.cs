using System;
using core.Shared;
using Newtonsoft.Json;

namespace storage.shared
{
    public class StoredAggregateEvent
    {
        static JsonSerializerSettings _formatting = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects
        };

        public string Entity { get; set; }
        public string UserId { get; set; }
        public string Key { get; set; }
        public DateTime Created { get; set; }
        public string EventJson
        {
            get
            {
                return JsonConvert.SerializeObject(this.Event, _formatting);
            }

            set
            {
                value = value.Replace("laimonas", "laimis@gmail.com");
                
                this.Event = JsonConvert.DeserializeObject<AggregateEvent>(value, _formatting);
            }
        }

        public int Version { get; set; }
        public AggregateEvent Event { get; set; }
    }
}
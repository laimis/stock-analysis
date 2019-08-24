using System;
using core.Portfolio;
using Newtonsoft.Json;

namespace storage
{
	internal class StoredAggregateEvent
	{
		static JsonSerializerSettings _formatting = new JsonSerializerSettings{
			TypeNameHandling = TypeNameHandling.Objects
		};

		public string UserId { get; set; }
		public string Ticker { get; set; }
		public DateTime Created { get; set; }
		public string EventJson
		{
			get
			{
				return JsonConvert.SerializeObject(this.Event, _formatting);
			}

			set
			{
				this.Event = JsonConvert.DeserializeObject<AggregateEvent>(value, _formatting);
			}
		}

		public int Version { get; set; }
		public AggregateEvent Event { get; set; }
	}
}
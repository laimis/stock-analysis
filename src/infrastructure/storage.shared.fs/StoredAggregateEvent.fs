namespace storage.shared

open System
open core.Shared
open Newtonsoft.Json

[<CLIMutable>]
type StoredAggregateEvent = {
    Entity: string
    UserId: Guid
    AggregateId: string
    Key: string
    Created: DateTimeOffset
    Version: int
    Event: AggregateEvent
    EventJson: string
}

module StoredAggregateEvent =
    
    let private formatting = JsonSerializerSettings(TypeNameHandling = TypeNameHandling.Objects)
    
    let serializeEvent (event: AggregateEvent) : string =
        JsonConvert.SerializeObject(event, formatting)
    
    let deserializeEvent (json: string) : AggregateEvent =
        let adjustedJson = EventInfraAdjustments.AdjustIfNeeded(json)
        JsonConvert.DeserializeObject<AggregateEvent>(adjustedJson, formatting)
    
    let create entity userId aggregateId key created version event =
        {
            Entity = entity
            UserId = userId
            AggregateId = aggregateId
            Key = key
            Created = created
            Version = version
            Event = event
            EventJson = serializeEvent event
        }

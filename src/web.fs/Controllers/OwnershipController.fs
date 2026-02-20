namespace web.Controllers

open System
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Authorization
open core.fs.Adapters.Storage
open core.Shared

[<CLIMutable>]
type SearchEntitiesRequest = {
    Name: string
}

[<CLIMutable>]
type BatchEntityIdsRequest = {
    Ids: Guid array
}

[<CLIMutable>]
type CreateEntityRequest = {
    Name: string
    EntityType: string
    Cik: string option
}

[<CLIMutable>]
type CreateRoleRequest = {
    EntityId: Guid
    CompanyTicker: string
    CompanyCik: string
    RelationshipType: string
    Title: string option
}

[<CLIMutable>]
type CreateOwnershipEventRequest = {
    EntityId: Guid
    CompanyTicker: string
    CompanyCik: string
    FilingId: Guid option
    EventType: string
    TransactionType: string option
    SharesBefore: int64 option
    SharesTransacted: int64 option
    SharesAfter: int64
    PercentOfClass: decimal option
    PricePerShare: decimal option
    TotalValue: decimal option
    TransactionDate: string
    FilingDate: string
    IsDirect: bool
    OwnershipNature: string option
}

[<ApiController>]
[<Authorize>]
[<Route("api/[controller]")>]
type OwnershipController(storage: IOwnershipStorage) =
    inherit ControllerBase()

    [<HttpGet("ticker/{ticker}")>]
    member this.GetOwnershipByTicker([<FromRoute>] ticker: string) = task {
        let t = Ticker ticker
        let! summary = storage.GetOwnershipSummary(t)
        return this.Ok summary
    }

    [<HttpGet("ticker/{ticker}/timeline")>]
    member this.GetOwnershipTimeline([<FromRoute>] ticker: string, [<FromQuery>] days: Nullable<int>) = task {
        let t = Ticker ticker
        let d = if days.HasValue then days.Value else 1825
        let! timeline = storage.GetOwnershipTimeline t d
        return this.Ok timeline
    }

    [<HttpGet("ticker/{ticker}/events")>]
    member this.GetEventsByTicker([<FromRoute>] ticker: string) = task {
        let t = Ticker ticker
        let! events = storage.GetEventsByCompany(t)
        return this.Ok events
    }

    [<HttpGet("entity/{entityId}")>]
    member this.GetEntity([<FromRoute>] entityId: Guid) = task {
        let! entity = storage.GetEntityById entityId
        match entity with
        | Some e -> return this.Ok(e) :> ActionResult
        | None -> return this.NotFound() :> ActionResult
    }

    [<HttpGet("entity/{entityId}/roles")>]
    member this.GetEntityRoles([<FromRoute>] entityId: Guid) = task {
        let! roles = storage.GetRolesByEntity entityId
        return this.Ok(roles)
    }

    [<HttpGet("entity/{entityId}/events")>]
    member this.GetEventsByEntity([<FromRoute>] entityId: Guid) = task {
        let! events = storage.GetEventsByEntity entityId
        return this.Ok(events)
    }

    [<HttpPost("entities/batch")>]
    member this.GetEntitiesBatch([<FromBody>] request: BatchEntityIdsRequest) = task {
        if isNull (box request) || request.Ids = null || request.Ids.Length = 0 then
            return this.Ok(Array.empty) :> ActionResult
        else
            let! entities = storage.GetEntitiesByIds(request.Ids)
            return this.Ok entities :> ActionResult
    }

    [<HttpGet("entities/search")>]
    member this.SearchEntities([<FromQuery>] name: string) = task {
        if String.IsNullOrWhiteSpace name then
            return this.BadRequest "Name parameter is required" :> ActionResult
        else
            let! entities = storage.FindEntitiesByName name
            return this.Ok entities :> ActionResult
    }

    [<HttpGet("timelines/recent")>]
    member this.GetRecentTimelines([<FromQuery>] limit: Nullable<int>) = task {
        let l = if limit.HasValue then limit.Value else 50
        let! timelines = storage.GetRecentTimelines l
        return this.Ok timelines
    }

    [<HttpPost("entity")>]
    member this.CreateEntity([<FromBody>] request: CreateEntityRequest) = task {
        let entity = {
            Id = Guid.NewGuid()
            Name = request.Name
            EntityType = request.EntityType
            Cik = request.Cik
            FirstSeen = DateTimeOffset.UtcNow
            LastSeen = DateTimeOffset.UtcNow
            CreatedAt = DateTimeOffset.UtcNow
        }
        let! entityId = storage.SaveEntity entity
        return this.Ok {| id = entityId |}
    }

    [<HttpPost("role")>]
    member this.CreateRole([<FromBody>] request: CreateRoleRequest) = task {
        let role = {
            Id = Guid.NewGuid()
            EntityId = request.EntityId
            CompanyTicker = request.CompanyTicker
            CompanyCik = request.CompanyCik
            RelationshipType = request.RelationshipType
            Title = request.Title
            IsActive = true
            FirstSeen = DateTimeOffset.UtcNow
            LastSeen = DateTimeOffset.UtcNow
        }
        let! roleId = storage.SaveRole role
        return this.Ok {| id = roleId |}
    }

    [<HttpPost("event")>]
    member this.CreateOwnershipEvent([<FromBody>] request: CreateOwnershipEventRequest) = task {
        let ownershipEvent = {
            Id = Guid.NewGuid()
            EntityId = request.EntityId
            CompanyTicker = request.CompanyTicker
            CompanyCik = request.CompanyCik
            FilingId = request.FilingId
            EventType = request.EventType
            TransactionType = request.TransactionType
            SharesBefore = request.SharesBefore
            SharesTransacted = request.SharesTransacted
            SharesAfter = request.SharesAfter
            PercentOfClass = request.PercentOfClass
            PricePerShare = request.PricePerShare
            TotalValue = request.TotalValue
            TransactionDate = request.TransactionDate
            FilingDate = request.FilingDate
            IsDirect = request.IsDirect
            OwnershipNature = request.OwnershipNature
            CreatedAt = DateTimeOffset.UtcNow
        }
        let! eventId = storage.SaveEvent ownershipEvent
        return this.Ok {| id = eventId |}
    }

    [<HttpGet("ticker/{ticker}/roles")>]
    member this.GetRolesByTicker([<FromRoute>] ticker: string) = task {
        let t = Ticker ticker
        let! roles = storage.GetRolesByCompany t
        return this.Ok roles
    }

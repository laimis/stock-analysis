namespace storage.postgres

open System
open System.Collections.Generic
open System.Data
open System.Linq
open System.Threading.Tasks
open core.fs.Adapters.Storage
open core.Shared
open Dapper
open Npgsql

// CLIMutable DTOs for Dapper query results
[<CLIMutable>]
type OwnershipEntityDto =
    {
        id: Guid
        name: string
        entity_type: string
        cik: string
        first_seen: DateTimeOffset
        last_seen: DateTimeOffset
        created_at: DateTimeOffset
    }

[<CLIMutable>]
type OwnershipEntityCompanyRoleDto =
    {
        id: Guid
        entity_id: Guid
        company_ticker: string
        company_cik: string
        relationship_type: string
        title: string
        is_active: bool
        first_seen: DateTimeOffset
        last_seen: DateTimeOffset
    }

[<CLIMutable>]
type OwnershipEventDto =
    {
        id: Guid
        entity_id: Guid
        company_ticker: string
        company_cik: string
        filing_id: Nullable<Guid>
        event_type: string
        transaction_type: string
        shares_before: Nullable<int64>
        shares_transacted: Nullable<int64>
        shares_after: int64
        percent_of_class: Nullable<decimal>
        price_per_share: Nullable<decimal>
        total_value: Nullable<decimal>
        transaction_date: string
        filing_date: string
        is_direct: bool
        ownership_nature: string
        created_at: DateTimeOffset
    }

[<CLIMutable>]
type OwnershipSummaryDto =
    {
        id: Guid
        name: string
        entity_type: string
        cik: string
        first_seen: DateTimeOffset
        last_seen: DateTimeOffset
        created_at: DateTimeOffset
        current_shares: int64
        percent_of_class: Nullable<decimal>
        last_updated: string
    }

type OwnershipStorage(connectionString: string) =
    
    let dataSource = (new NpgsqlDataSourceBuilder(connectionString)).Build()
    
    member private _.GetConnection() : IDbConnection =
        dataSource.OpenConnection()
    
    // Helper to map DTO to domain entity
    member private _.MapToOwnershipEntity(dto: OwnershipEntityDto) : OwnershipEntity =
        {
            Id = dto.id
            Name = dto.name
            EntityType = dto.entity_type
            Cik = Option.ofObj dto.cik
            FirstSeen = dto.first_seen
            LastSeen = dto.last_seen
            CreatedAt = dto.created_at
        }
    
    member private _.MapToOwnershipEntityCompanyRole(dto: OwnershipEntityCompanyRoleDto) : OwnershipEntityCompanyRole =
        {
            Id = dto.id
            EntityId = dto.entity_id
            CompanyTicker = dto.company_ticker
            CompanyCik = dto.company_cik
            RelationshipType = dto.relationship_type
            Title = Option.ofObj dto.title
            IsActive = dto.is_active
            FirstSeen = dto.first_seen
            LastSeen = dto.last_seen
        }
    
    member private _.MapToOwnershipEvent(dto: OwnershipEventDto) : OwnershipEvent =
        {
            Id = dto.id
            EntityId = dto.entity_id
            CompanyTicker = dto.company_ticker
            CompanyCik = dto.company_cik
            FilingId = if dto.filing_id.HasValue then Some dto.filing_id.Value else None
            EventType = dto.event_type
            TransactionType = Option.ofObj dto.transaction_type
            SharesBefore = if dto.shares_before.HasValue then Some dto.shares_before.Value else None
            SharesTransacted = if dto.shares_transacted.HasValue then Some dto.shares_transacted.Value else None
            SharesAfter = dto.shares_after
            PercentOfClass = if dto.percent_of_class.HasValue then Some dto.percent_of_class.Value else None
            PricePerShare = if dto.price_per_share.HasValue then Some dto.price_per_share.Value else None
            TotalValue = if dto.total_value.HasValue then Some dto.total_value.Value else None
            TransactionDate = dto.transaction_date
            FilingDate = dto.filing_date
            IsDirect = dto.is_direct
            OwnershipNature = Option.ofObj dto.ownership_nature
            CreatedAt = dto.created_at
        }
    
    interface IOwnershipStorage with
        
        // Entity Management
        
        member this.GetEntityById(entityId: Guid) =
            task {
                use db = this.GetConnection()
                
                let query = """
                    SELECT id, name, entity_type, cik,
                           first_seen, last_seen, created_at
                    FROM ownership_entities
                    WHERE id = @EntityId"""
                
                let! dto = db.QueryFirstOrDefaultAsync<OwnershipEntityDto>(query, {| EntityId = entityId |})
                
                if isNull (box dto) then
                    return None
                else
                    return Some (this.MapToOwnershipEntity(dto))
            }
        
        member this.FindEntityByCik(cik: string) =
            task {
                use db = this.GetConnection()
                
                let query = """
                    SELECT id, name, entity_type, cik,
                           first_seen, last_seen, created_at
                    FROM ownership_entities
                    WHERE cik = @Cik"""
                
                let! dto = db.QueryFirstOrDefaultAsync<OwnershipEntityDto>(query, {| Cik = cik |})
                
                if isNull (box dto) then
                    return None
                else
                    return Some (this.MapToOwnershipEntity(dto))
            }
        
        member this.FindEntitiesByName(name: string) =
            task {
                use db = this.GetConnection()
                
                let query = """
                    SELECT id, name, entity_type, cik,
                           first_seen, last_seen, created_at
                    FROM ownership_entities
                    WHERE name ILIKE @Name
                    ORDER BY name"""
                
                let searchPattern = sprintf "%%%s%%" name
                let! dtos = db.QueryAsync<OwnershipEntityDto>(query, {| Name = searchPattern |})
                
                return dtos |> Seq.map this.MapToOwnershipEntity
            }
        
        member this.SaveEntity(entity: OwnershipEntity) =
            task {
                use db = this.GetConnection()
                
                let query = """
                    INSERT INTO ownership_entities (id, name, entity_type, cik, first_seen, last_seen, created_at)
                    VALUES (@Id, @Name, @EntityType, @Cik, @FirstSeen, @LastSeen, @CreatedAt)
                    ON CONFLICT (id)
                    DO UPDATE SET 
                        name = EXCLUDED.name,
                        cik = EXCLUDED.cik,
                        entity_type = EXCLUDED.entity_type,
                        last_seen = EXCLUDED.last_seen
                    RETURNING id"""
                
                let! result = db.QuerySingleAsync<Guid>(query, {|
                    Id = entity.Id
                    Name = entity.Name
                    EntityType = entity.EntityType
                    Cik = Option.toObj entity.Cik
                    FirstSeen = entity.FirstSeen
                    LastSeen = entity.LastSeen
                    CreatedAt = entity.CreatedAt
                |})
                
                return result
            }
        
        member this.UpdateEntityLastSeen entityId lastSeen =
            task {
                use db = this.GetConnection()
                
                let query = """
                    UPDATE ownership_entities
                    SET last_seen = @LastSeen
                    WHERE id = @EntityId"""
                
                do! db.ExecuteAsync(query, {| EntityId = entityId; LastSeen = lastSeen |}) :> Task
            }
        
        // Entity-Company Role Management
        
        member this.GetRolesByEntity(entityId: Guid) =
            task {
                use db = this.GetConnection()
                
                let query = """
                    SELECT id, entity_id, company_ticker,
                           company_cik, relationship_type,
                           title, is_active, first_seen,
                           last_seen
                    FROM ownership_entity_company_roles
                    WHERE entity_id = @EntityId
                    ORDER BY last_seen DESC"""
                
                let! dtos = db.QueryAsync<OwnershipEntityCompanyRoleDto>(query, {| EntityId = entityId |})
                
                return dtos |> Seq.map this.MapToOwnershipEntityCompanyRole
            }
        
        member this.GetRolesByCompany(ticker: Ticker) =
            task {
                use db = this.GetConnection()
                
                let query = """
                    SELECT id, entity_id, company_ticker,
                           company_cik, relationship_type,
                           title, is_active, first_seen,
                           last_seen
                    FROM ownership_entity_company_roles
                    WHERE company_ticker = @Ticker AND is_active = true
                    ORDER BY last_seen DESC"""
                
                let! dtos = db.QueryAsync<OwnershipEntityCompanyRoleDto>(query, {| Ticker = ticker.Value |})
                
                return dtos |> Seq.map this.MapToOwnershipEntityCompanyRole
            }
        
        member this.SaveRole(role: OwnershipEntityCompanyRole) =
            task {
                use db = this.GetConnection()
                
                let query = """
                    INSERT INTO ownership_entity_company_roles 
                        (id, entity_id, company_ticker, company_cik, relationship_type, title, 
                         is_active, first_seen, last_seen)
                    VALUES (@Id, @EntityId, @CompanyTicker, @CompanyCik, @RelationshipType, @Title,
                            @IsActive, @FirstSeen, @LastSeen)
                    ON CONFLICT (entity_id, company_ticker, relationship_type) WHERE is_active = true
                    DO UPDATE SET 
                        title = EXCLUDED.title,
                        last_seen = EXCLUDED.last_seen
                    RETURNING id"""
                
                let! result = db.QuerySingleAsync<Guid>(query, {|
                    Id = role.Id
                    EntityId = role.EntityId
                    CompanyTicker = role.CompanyTicker
                    CompanyCik = role.CompanyCik
                    RelationshipType = role.RelationshipType
                    Title = Option.toObj role.Title
                    IsActive = role.IsActive
                    FirstSeen = role.FirstSeen
                    LastSeen = role.LastSeen
                |})
                
                return result
            }
        
        member this.DeactivateRole(roleId: Guid) =
            task {
                use db = this.GetConnection()
                
                let query = """
                    UPDATE ownership_entity_company_roles
                    SET is_active = false
                    WHERE id = @RoleId"""
                
                do! db.ExecuteAsync(query, {| RoleId = roleId |}) :> Task
            }
        
        // Ownership Event Management
        
        member this.SaveEvent(ownershipEvent: OwnershipEvent) =
            task {
                use db = this.GetConnection()
                
                let query = """
                    INSERT INTO ownership_events 
                        (id, entity_id, company_ticker, company_cik, filing_id, event_type,
                         transaction_type, shares_before, shares_transacted, shares_after,
                         percent_of_class, price_per_share, total_value, transaction_date,
                         filing_date, is_direct, ownership_nature, created_at)
                    VALUES (@Id, @EntityId, @CompanyTicker, @CompanyCik, @FilingId, @EventType,
                            @TransactionType, @SharesBefore, @SharesTransacted, @SharesAfter,
                            @PercentOfClass, @PricePerShare, @TotalValue, @TransactionDate,
                            @FilingDate, @IsDirect, @OwnershipNature, @CreatedAt)
                    RETURNING id"""
                
                let! result = db.QuerySingleAsync<Guid>(query, {|
                    Id = ownershipEvent.Id
                    EntityId = ownershipEvent.EntityId
                    CompanyTicker = ownershipEvent.CompanyTicker
                    CompanyCik = ownershipEvent.CompanyCik
                    FilingId = Option.toNullable ownershipEvent.FilingId
                    EventType = ownershipEvent.EventType
                    TransactionType = Option.toObj ownershipEvent.TransactionType
                    SharesBefore = Option.toNullable ownershipEvent.SharesBefore
                    SharesTransacted = Option.toNullable ownershipEvent.SharesTransacted
                    SharesAfter = ownershipEvent.SharesAfter
                    PercentOfClass = Option.toNullable ownershipEvent.PercentOfClass
                    PricePerShare = Option.toNullable ownershipEvent.PricePerShare
                    TotalValue = Option.toNullable ownershipEvent.TotalValue
                    TransactionDate = ownershipEvent.TransactionDate
                    FilingDate = ownershipEvent.FilingDate
                    IsDirect = ownershipEvent.IsDirect
                    OwnershipNature = Option.toObj ownershipEvent.OwnershipNature
                    CreatedAt = ownershipEvent.CreatedAt
                |})
                
                return result
            }
        
        member this.SaveEvents(events: seq<OwnershipEvent>) =
            task {
                use db = this.GetConnection()
                
                let query = """
                    INSERT INTO ownership_events 
                        (id, entity_id, company_ticker, company_cik, filing_id, event_type,
                         transaction_type, shares_before, shares_transacted, shares_after,
                         percent_of_class, price_per_share, total_value, transaction_date,
                         filing_date, is_direct, ownership_nature, created_at)
                    VALUES (@Id, @EntityId, @CompanyTicker, @CompanyCik, @FilingId, @EventType,
                            @TransactionType, @SharesBefore, @SharesTransacted, @SharesAfter,
                            @PercentOfClass, @PricePerShare, @TotalValue, @TransactionDate,
                            @FilingDate, @IsDirect, @OwnershipNature, @CreatedAt)"""
                
                let eventsParams = 
                    events 
                    |> Seq.map (fun e -> 
                        {|
                            Id = e.Id
                            EntityId = e.EntityId
                            CompanyTicker = e.CompanyTicker
                            CompanyCik = e.CompanyCik
                            FilingId = Option.toNullable e.FilingId
                            EventType = e.EventType
                            TransactionType = Option.toObj e.TransactionType
                            SharesBefore = Option.toNullable e.SharesBefore
                            SharesTransacted = Option.toNullable e.SharesTransacted
                            SharesAfter = e.SharesAfter
                            PercentOfClass = Option.toNullable e.PercentOfClass
                            PricePerShare = Option.toNullable e.PricePerShare
                            TotalValue = Option.toNullable e.TotalValue
                            TransactionDate = e.TransactionDate
                            FilingDate = e.FilingDate
                            IsDirect = e.IsDirect
                            OwnershipNature = Option.toObj e.OwnershipNature
                            CreatedAt = e.CreatedAt
                        |})
                    |> Seq.toArray
                
                let! rowsAffected = db.ExecuteAsync(query, eventsParams)
                
                return rowsAffected
            }
        
        member this.GetEventsByCompany(ticker: Ticker) =
            task {
                use db = this.GetConnection()
                
                let query = """
                    SELECT id, entity_id, company_ticker,
                           company_cik, filing_id, event_type,
                           transaction_type, shares_before,
                           shares_transacted, shares_after,
                           percent_of_class, price_per_share,
                           total_value, transaction_date,
                           filing_date, is_direct,
                           ownership_nature, created_at
                    FROM ownership_events
                    WHERE company_ticker = @Ticker
                    ORDER BY transaction_date DESC"""
                
                let! dtos = db.QueryAsync<OwnershipEventDto>(query, {| Ticker = ticker.Value |})
                
                return dtos |> Seq.map this.MapToOwnershipEvent
            }
        
        member this.GetEventsByCompanyDateRange ticker startDate endDate =
            task {
                use db = this.GetConnection()
                
                let query = """
                    SELECT id, entity_id, company_ticker,
                           company_cik, filing_id, event_type,
                           transaction_type, shares_before,
                           shares_transacted, shares_after,
                           percent_of_class, price_per_share,
                           total_value, transaction_date,
                           filing_date, is_direct,
                           ownership_nature, created_at
                    FROM ownership_events
                    WHERE company_ticker = @Ticker 
                      AND transaction_date >= @StartDate 
                      AND transaction_date <= @EndDate
                    ORDER BY transaction_date DESC"""
                
                let! dtos = db.QueryAsync<OwnershipEventDto>(query, {| 
                    Ticker = ticker.Value
                    StartDate = startDate
                    EndDate = endDate
                |})
                
                return dtos |> Seq.map this.MapToOwnershipEvent
            }
        
        member this.GetEventsByEntity(entityId: Guid) =
            task {
                use db = this.GetConnection()
                
                let query = """
                    SELECT id, entity_id, company_ticker,
                           company_cik, filing_id, event_type,
                           transaction_type, shares_before,
                           shares_transacted, shares_after,
                           percent_of_class, price_per_share,
                           total_value, transaction_date,
                           filing_date, is_direct,
                           ownership_nature, created_at
                    FROM ownership_events
                    WHERE entity_id = @EntityId
                    ORDER BY transaction_date DESC"""
                
                let! dtos = db.QueryAsync<OwnershipEventDto>(query, {| EntityId = entityId |})
                
                return dtos |> Seq.map this.MapToOwnershipEvent
            }
        
        member this.GetLatestEventForEntityCompany entityId ticker =
            task {
                use db = this.GetConnection()
                
                let query = """
                    SELECT id, entity_id, company_ticker,
                           company_cik, filing_id, event_type,
                           transaction_type, shares_before,
                           shares_transacted, shares_after,
                           percent_of_class, price_per_share,
                           total_value, transaction_date,
                           filing_date, is_direct,
                           ownership_nature, created_at
                    FROM ownership_events
                    WHERE entity_id = @EntityId AND company_ticker = @Ticker
                    ORDER BY transaction_date DESC
                    LIMIT 1"""
                
                let! dto = db.QueryFirstOrDefaultAsync<OwnershipEventDto>(query, {| 
                    EntityId = entityId
                    Ticker = ticker.Value
                |})
                
                if isNull (box dto) then
                    return None
                else
                    return Some (this.MapToOwnershipEvent(dto))
            }
        
        // Summary/Analytics
        
        member this.GetOwnershipSummary(ticker: Ticker) =
            task {
                use db = this.GetConnection()
                
                let query = """
                    WITH latest_events AS (
                        SELECT DISTINCT ON (entity_id)
                            entity_id, shares_after, percent_of_class, transaction_date
                        FROM ownership_events
                        WHERE company_ticker = @Ticker
                        ORDER BY entity_id, transaction_date DESC
                    )
                    SELECT 
                        e.id, e.name, e.entity_type, e.cik,
                        e.first_seen, e.last_seen, e.created_at,
                        le.shares_after as current_shares,
                        le.percent_of_class,
                        le.transaction_date as last_updated
                    FROM ownership_entities e
                    INNER JOIN latest_events le ON e.id = le.entity_id
                    WHERE le.shares_after > 0
                    ORDER BY le.shares_after DESC"""
                
                let! dtos = db.QueryAsync<OwnershipSummaryDto>(query, {| Ticker = ticker.Value |})
                
                // Convert to OwnershipSummary by fetching roles for each entity
                let! summaries = 
                    dtos
                    |> Seq.map (fun dto -> async {
                        let entity = {
                            Id = dto.id
                            Name = dto.name
                            EntityType = dto.entity_type
                            Cik = Option.ofObj dto.cik
                            FirstSeen = dto.first_seen
                            LastSeen = dto.last_seen
                            CreatedAt = dto.created_at
                        }
                        
                        // Get roles for this entity
                        // TODO: This is N+1 query, consider optimizing with a single query that aggregates roles
                        let storage = this :> IOwnershipStorage
                        let! roles = storage.GetRolesByEntity(entity.Id) |> Async.AwaitTask
                        let rolesList = roles |> List.ofSeq
                        
                        let percentOption = 
                            if dto.percent_of_class.HasValue then 
                                Some dto.percent_of_class.Value 
                            else 
                                None
                        
                        return {
                            Entity = entity
                            Roles = rolesList
                            CurrentShares = dto.current_shares
                            PercentOfClass = percentOption
                            LastUpdated = DateTimeOffset.Parse(dto.last_updated)
                        }:OwnershipSummary
                    })
                    |> Async.Sequential
                
                return summaries |> Seq.ofArray
            }
        
        member this.GetOwnershipTimeline ticker days =
            task {
                use db = this.GetConnection()
                
                let cutoffDate = DateTimeOffset.UtcNow.AddDays(float -days).ToString("yyyy-MM-dd")
                
                let query = """
                    SELECT id, entity_id, company_ticker,
                           company_cik, filing_id, event_type,
                           transaction_type, shares_before,
                           shares_transacted, shares_after,
                           percent_of_class, price_per_share,
                           total_value, transaction_date,
                           filing_date, is_direct,
                           ownership_nature, created_at
                    FROM ownership_events
                    WHERE company_ticker = @Ticker AND transaction_date >= @CutoffDate
                    ORDER BY transaction_date DESC"""
                
                let! dtos = db.QueryAsync<OwnershipEventDto>(query, {| 
                    Ticker = ticker.Value
                    CutoffDate = cutoffDate
                |})
                
                return dtos |> Seq.map this.MapToOwnershipEvent
            }
        
        member this.GetRecentTimelines limit =
            task {
                use db = this.GetConnection()
                
                let query = """
                    SELECT id, entity_id, company_ticker,
                           company_cik, filing_id, event_type,
                           transaction_type, shares_before,
                           shares_transacted, shares_after,
                           percent_of_class, price_per_share,
                           total_value, transaction_date,
                           filing_date, is_direct,
                           ownership_nature, created_at
                    FROM ownership_events
                    ORDER BY filing_date DESC
                    LIMIT @Limit"""
                
                let! dtos = db.QueryAsync<OwnershipEventDto>(query, {| Limit = limit |})
                
                return dtos |> Seq.map this.MapToOwnershipEvent
            }

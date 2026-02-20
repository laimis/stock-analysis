namespace core.fs.Adapters.Storage

open System
open System.Collections.Generic
open System.Threading.Tasks
open core.Shared

/// Represents an ownership entity (institution, individual, director, etc.)
[<CLIMutable>]
type OwnershipEntity =
    {
        Id: Guid
        Name: string
        EntityType: string // 'individual', 'institution', 'fund', 'company'
        Cik: string option
        FirstSeen: DateTimeOffset
        LastSeen: DateTimeOffset
        CreatedAt: DateTimeOffset
    }

/// Represents an entity's relationship to a company
[<CLIMutable>]
type OwnershipEntityCompanyRole =
    {
        Id: Guid
        EntityId: Guid
        CompanyTicker: string
        CompanyCik: string
        RelationshipType: string // 'director', 'officer', 'beneficial_owner', 'institutional_holder', 'ten_percent_owner'
        Title: string option // specific role: "CEO", "CFO", "Board Member", etc.
        IsActive: bool
        FirstSeen: DateTimeOffset
        LastSeen: DateTimeOffset
    }

/// Represents an ownership transaction or position update
[<CLIMutable>]
type OwnershipEvent =
    {
        Id: Guid
        EntityId: Guid
        CompanyTicker: string
        CompanyCik: string
        FilingId: Guid option
        EventType: string // 'transaction', 'position_disclosure', 'beneficial_ownership_update'
        TransactionType: string option // 'purchase', 'sale', 'grant', 'exercise', 'gift'
        SharesBefore: int64 option
        SharesTransacted: int64 option
        SharesAfter: int64
        PercentOfClass: decimal option
        PricePerShare: decimal option
        TotalValue: decimal option
        TransactionDate: string
        FilingDate: string
        IsDirect: bool
        OwnershipNature: string option // "sole voting power", "shared voting power", etc.
        CreatedAt: DateTimeOffset
    }

/// Summary of entity ownership for a company at a point in time
type OwnershipSummary =
    {
        Entity: OwnershipEntity
        Roles: OwnershipEntityCompanyRole list
        CurrentShares: int64
        PercentOfClass: decimal option
        LastUpdated: DateTimeOffset
    }

type IOwnershipStorage =
    // Entity Management
    
    /// Get an entity by ID
    abstract member GetEntityById : entityId:Guid -> Task<OwnershipEntity option>
    
    /// Get multiple entities by their IDs in one query
    abstract member GetEntitiesByIds : entityIds:seq<Guid> -> Task<IEnumerable<OwnershipEntity>>
    
    /// Find an entity by CIK (unique identifier)
    abstract member FindEntityByCik : cik:string -> Task<OwnershipEntity option>
    
    /// Find an entity by name (for fuzzy matching)
    abstract member FindEntitiesByName : name:string -> Task<IEnumerable<OwnershipEntity>>
    
    /// Save a new entity (or update if CIK already exists)
    abstract member SaveEntity : entity:OwnershipEntity -> Task<Guid>
    
    /// Update entity's last seen timestamp
    abstract member UpdateEntityLastSeen : entityId:Guid -> lastSeen:DateTimeOffset -> Task<unit>
    
    // Entity-Company Role Management
    
    /// Get all roles for an entity
    abstract member GetRolesByEntity : entityId:Guid -> Task<IEnumerable<OwnershipEntityCompanyRole>>
    
    /// Get all active roles for a company
    abstract member GetRolesByCompany : ticker:Ticker -> Task<IEnumerable<OwnershipEntityCompanyRole>>
    
    /// Save or update a role relationship
    abstract member SaveRole : role:OwnershipEntityCompanyRole -> Task<Guid>
    
    /// Deactivate a role (set is_active = false)
    abstract member DeactivateRole : roleId:Guid -> Task<unit>
    
    // Ownership Event Management
    
    /// Save an ownership event
    abstract member SaveEvent : event:OwnershipEvent -> Task<Guid>
    
    /// Save multiple ownership events
    abstract member SaveEvents : events:seq<OwnershipEvent> -> Task<int>
    
    /// Get all ownership events for a company
    abstract member GetEventsByCompany : ticker:Ticker -> Task<IEnumerable<OwnershipEvent>>
    
    /// Get ownership events for a company within date range
    abstract member GetEventsByCompanyDateRange : ticker:Ticker -> startDate:string -> endDate:string -> Task<IEnumerable<OwnershipEvent>>
    
    /// Get all ownership events for an entity
    abstract member GetEventsByEntity : entityId:Guid -> Task<IEnumerable<OwnershipEvent>>
    
    /// Get the most recent ownership event for an entity-company pair
    abstract member GetLatestEventForEntityCompany : entityId:Guid -> ticker:Ticker -> Task<OwnershipEvent option>
    
    // Summary/Analytics
    
    /// Get current ownership summary for a company
    abstract member GetOwnershipSummary : ticker:Ticker -> Task<IEnumerable<OwnershipSummary>>
    
    /// Get ownership timeline for a company (aggregated by date)
    abstract member GetOwnershipTimeline : ticker:Ticker -> days:int -> Task<IEnumerable<OwnershipEvent>>
    
    /// Get the most recently created ownership timelines across all companies
    abstract member GetRecentTimelines : limit:int -> Task<IEnumerable<OwnershipEvent>>

module OwnershipEntity =
    let create name entityType cik =
        {
            Id = Guid.NewGuid()
            Name = name
            EntityType = entityType
            Cik = cik
            FirstSeen = DateTimeOffset.UtcNow
            LastSeen = DateTimeOffset.UtcNow
            CreatedAt = DateTimeOffset.UtcNow
        }

module OwnershipEntityCompanyRole =
    let create entityId companyTicker companyCik relationshipType title =
        {
            Id = Guid.NewGuid()
            EntityId = entityId
            CompanyTicker = companyTicker
            CompanyCik = companyCik
            RelationshipType = relationshipType
            Title = title
            IsActive = true
            FirstSeen = DateTimeOffset.UtcNow
            LastSeen = DateTimeOffset.UtcNow
        }

module OwnershipEvent =
    let create entityId companyTicker companyCik filingId eventType transactionType 
               sharesBefore sharesTransacted sharesAfter percentOfClass pricePerShare 
               totalValue transactionDate filingDate isDirect ownershipNature =
        {
            Id = Guid.NewGuid()
            EntityId = entityId
            CompanyTicker = companyTicker
            CompanyCik = companyCik
            FilingId = filingId
            EventType = eventType
            TransactionType = transactionType
            SharesBefore = sharesBefore
            SharesTransacted = sharesTransacted
            SharesAfter = sharesAfter
            PercentOfClass = percentOfClass
            PricePerShare = pricePerShare
            TotalValue = totalValue
            TransactionDate = transactionDate
            FilingDate = filingDate
            IsDirect = isDirect
            OwnershipNature = ownershipNature
            CreatedAt = DateTimeOffset.UtcNow
        }

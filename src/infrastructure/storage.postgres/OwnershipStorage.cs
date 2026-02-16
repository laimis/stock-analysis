using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.fs.Adapters.Storage;
using core.Shared;
using Dapper;
using Npgsql;
using Microsoft.FSharp.Core;

namespace storage.postgres
{
    public class OwnershipStorage : IOwnershipStorage
    {
        private readonly string _connectionString;

        public OwnershipStorage(string connectionString)
        {
            _connectionString = connectionString;
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        // Helper methods to convert F# options to nullable types for Dapper
        private static T? ToNullable<T>(FSharpOption<T> option) where T : struct
        {
            return OptionModule.IsSome(option) ? (T?)option.Value : null;
        }

        private static string? ToNullableString(FSharpOption<string> option)
        {
            return OptionModule.ToObj(option);
        }

        // Helper to convert DateTime (from Postgres) to DateTimeOffset
        private static DateTimeOffset ToDateTimeOffset(object value)
        {
            if (value is DateTimeOffset dto)
                return dto;
            if (value is DateTime dt)
                return new DateTimeOffset(dt, TimeSpan.Zero); // Postgres returns UTC
            throw new InvalidCastException($"Cannot convert {value.GetType()} to DateTimeOffset");
        }

        // Helper methods to map dynamic database rows to F# types
        private static OwnershipEntity MapToOwnershipEntity(dynamic row)
        {
            var rowDict = (IDictionary<string, object>)row;
            return new OwnershipEntity
            {
                Id = (Guid)rowDict["id"],
                Name = (string)rowDict["name"],
                EntityType = (string)rowDict["entity_type"],
                Cik = rowDict["cik"] is string cik
                    ? FSharpOption<string>.Some(cik) 
                    : FSharpOption<string>.None,
                FirstSeen = ToDateTimeOffset(rowDict["first_seen"]),
                LastSeen = ToDateTimeOffset(rowDict["last_seen"]),
                CreatedAt = ToDateTimeOffset(rowDict["created_at"])
            };
        }

        private static OwnershipEntityCompanyRole MapToOwnershipEntityCompanyRole(dynamic row)
        {
            var rowDict = (IDictionary<string, object>)row;
            return new OwnershipEntityCompanyRole
            {
                Id = (Guid)rowDict["id"],
                EntityId = (Guid)rowDict["entity_id"],
                CompanyTicker = (string)rowDict["company_ticker"],
                CompanyCik = (string)rowDict["company_cik"],
                RelationshipType = (string)rowDict["relationship_type"],
                Title = rowDict["title"] is string title
                    ? FSharpOption<string>.Some(title) 
                    : FSharpOption<string>.None,
                IsActive = (bool)rowDict["is_active"],
                FirstSeen = ToDateTimeOffset(rowDict["first_seen"]),
                LastSeen = ToDateTimeOffset(rowDict["last_seen"])
            };
        }

        private static OwnershipEvent MapToOwnershipEvent(dynamic row)
        {
            var rowDict = (IDictionary<string, object>)row;
            return new OwnershipEvent
            {
                Id = (Guid)rowDict["id"],
                EntityId = (Guid)rowDict["entity_id"],
                CompanyTicker = (string)rowDict["company_ticker"],
                CompanyCik = (string)rowDict["company_cik"],
                FilingId = rowDict["filing_id"] is Guid filingId
                    ? FSharpOption<Guid>.Some(filingId) 
                    : FSharpOption<Guid>.None,
                EventType = (string)rowDict["event_type"],
                TransactionType = rowDict["transaction_type"] is string transactionType
                    ? FSharpOption<string>.Some(transactionType) 
                    : FSharpOption<string>.None,
                SharesBefore = rowDict["shares_before"] is long sharesBefore
                    ? FSharpOption<long>.Some(sharesBefore) 
                    : FSharpOption<long>.None,
                SharesTransacted = rowDict["shares_transacted"] is long sharesTransacted
                    ? FSharpOption<long>.Some(sharesTransacted) 
                    : FSharpOption<long>.None,
                SharesAfter = (long)rowDict["shares_after"],
                PercentOfClass = rowDict["percent_of_class"] is decimal percentOfClass
                    ? FSharpOption<decimal>.Some(percentOfClass) 
                    : FSharpOption<decimal>.None,
                PricePerShare = rowDict["price_per_share"] is decimal pricePerShare
                    ? FSharpOption<decimal>.Some(pricePerShare) 
                    : FSharpOption<decimal>.None,
                TotalValue = rowDict["total_value"] is decimal totalValue
                    ? FSharpOption<decimal>.Some(totalValue) 
                    : FSharpOption<decimal>.None,
                TransactionDate = (string)rowDict["transaction_date"],
                FilingDate = (string)rowDict["filing_date"],
                IsDirect = (bool)rowDict["is_direct"],
                OwnershipNature = rowDict["ownership_nature"] is string ownershipNature
                    ? FSharpOption<string>.Some(ownershipNature) 
                    : FSharpOption<string>.None,
                CreatedAt = ToDateTimeOffset(rowDict["created_at"])
            };
        }

        // Entity Management

        public async Task<FSharpOption<OwnershipEntity>> GetEntityById(Guid entityId)
        {
            using var db = GetConnection();
            
            var query = @"
                SELECT id, name, entity_type, cik,
                       first_seen, last_seen, created_at
                FROM ownership_entities
                WHERE id = @EntityId";

            var row = await db.QueryFirstOrDefaultAsync(query, new { EntityId = entityId });
            if (row == null)
                return FSharpOption<OwnershipEntity>.None;

            var entity = MapToOwnershipEntity(row);
            return FSharpOption<OwnershipEntity>.Some(entity);
        }

        public async Task<FSharpOption<OwnershipEntity>> FindEntityByCik(string cik)
        {
            using var db = GetConnection();
            
            var query = @"
                SELECT id, name, entity_type, cik,
                       first_seen, last_seen, created_at
                FROM ownership_entities
                WHERE cik = @Cik";

            var row = await db.QueryFirstOrDefaultAsync(query, new { Cik = cik });
            if (row == null)
                return FSharpOption<OwnershipEntity>.None;

            var entity = MapToOwnershipEntity(row);
            return FSharpOption<OwnershipEntity>.Some(entity);
        }

        public async Task<IEnumerable<OwnershipEntity>> FindEntitiesByName(string name)
        {
            using var db = GetConnection();
            
            // Use ILIKE for case-insensitive search and % wildcards for fuzzy matching
            var query = @"
                SELECT id, name, entity_type, cik,
                       first_seen, last_seen, created_at
                FROM ownership_entities
                WHERE name ILIKE @Name
                ORDER BY name";

            var searchPattern = $"%{name}%";
            var rows = await db.QueryAsync(query, new { Name = $"%{name}%" });
            return rows.Select(MapToOwnershipEntity);
        }

        public async Task<Guid> SaveEntity(OwnershipEntity entity)
        {
            using var db = GetConnection();
            
            // Upsert: If ID exists, update last_seen; otherwise insert new
            var query = @"
                INSERT INTO ownership_entities (id, name, entity_type, cik, first_seen, last_seen, created_at)
                VALUES (@Id, @Name, @EntityType, @Cik, @FirstSeen, @LastSeen, @CreatedAt)
                ON CONFLICT (id)
                DO UPDATE SET 
                    name = EXCLUDED.name,
                    cik = EXCLUDED.cik,
                    entity_type = EXCLUDED.entity_type,
                    last_seen = EXCLUDED.last_seen
                RETURNING id";

            var result = await db.QuerySingleAsync<Guid>(query, new
            {
                Id = entity.Id,
                Name = entity.Name,
                EntityType = entity.EntityType,
                Cik = ToNullableString(entity.Cik),
                FirstSeen = entity.FirstSeen,
                LastSeen = entity.LastSeen,
                CreatedAt = entity.CreatedAt
            });

            return result;
        }

        public async Task<Unit> UpdateEntityLastSeen(Guid entityId, DateTimeOffset lastSeen)
        {
            using var db = GetConnection();
            
            var query = @"
                UPDATE ownership_entities
                SET last_seen = @LastSeen
                WHERE id = @EntityId";

            await db.ExecuteAsync(query, new { EntityId = entityId, LastSeen = lastSeen });
            return null!;
        }

        // Entity-Company Role Management

        public async Task<IEnumerable<OwnershipEntityCompanyRole>> GetRolesByEntity(Guid entityId)
        {
            using var db = GetConnection();
            
            var query = @"
                SELECT id, entity_id, company_ticker,
                       company_cik, relationship_type,
                       title, is_active, first_seen,
                       last_seen
                FROM ownership_entity_company_roles
                WHERE entity_id = @EntityId
                ORDER BY last_seen DESC";

            var rows = await db.QueryAsync(query, new { EntityId = entityId });
            return rows.Select(MapToOwnershipEntityCompanyRole);
        }

        public async Task<IEnumerable<OwnershipEntityCompanyRole>> GetRolesByCompany(Ticker ticker)
        {
            using var db = GetConnection();
            
            var query = @"
                SELECT id, entity_id, company_ticker,
                       company_cik, relationship_type,
                       title, is_active, first_seen,
                       last_seen
                FROM ownership_entity_company_roles
                WHERE company_ticker = @Ticker AND is_active = true
                ORDER BY last_seen DESC";

            var rows = await db.QueryAsync(query, new { Ticker = ticker.Value });
            return rows.Select(MapToOwnershipEntityCompanyRole);
        }

        public async Task<Guid> SaveRole(OwnershipEntityCompanyRole role)
        {
            using var db = GetConnection();
            
            var query = @"
                INSERT INTO ownership_entity_company_roles 
                    (id, entity_id, company_ticker, company_cik, relationship_type, title, 
                     is_active, first_seen, last_seen)
                VALUES (@Id, @EntityId, @CompanyTicker, @CompanyCik, @RelationshipType, @Title,
                        @IsActive, @FirstSeen, @LastSeen)
                ON CONFLICT (entity_id, company_ticker, relationship_type) WHERE is_active = true
                DO UPDATE SET 
                    title = EXCLUDED.title,
                    last_seen = EXCLUDED.last_seen
                RETURNING id";

            var result = await db.QuerySingleAsync<Guid>(query, new
            {
                Id = role.Id,
                EntityId = role.EntityId,
                CompanyTicker = role.CompanyTicker,
                CompanyCik = role.CompanyCik,
                RelationshipType = role.RelationshipType,
                Title = ToNullableString(role.Title),
                IsActive = role.IsActive,
                FirstSeen = role.FirstSeen,
                LastSeen = role.LastSeen
            });

            return result;
        }

        public async Task<Unit> DeactivateRole(Guid roleId)
        {
            using var db = GetConnection();
            
            var query = @"
                UPDATE ownership_entity_company_roles
                SET is_active = false
                WHERE id = @RoleId";

            await db.ExecuteAsync(query, new { RoleId = roleId });
            return null!;
        }

        // Ownership Event Management

        public async Task<Guid> SaveEvent(OwnershipEvent ownershipEvent)
        {
            using var db = GetConnection();
            
            var query = @"
                INSERT INTO ownership_events 
                    (id, entity_id, company_ticker, company_cik, filing_id, event_type,
                     transaction_type, shares_before, shares_transacted, shares_after,
                     percent_of_class, price_per_share, total_value, transaction_date,
                     filing_date, is_direct, ownership_nature, created_at)
                VALUES (@Id, @EntityId, @CompanyTicker, @CompanyCik, @FilingId, @EventType,
                        @TransactionType, @SharesBefore, @SharesTransacted, @SharesAfter,
                        @PercentOfClass, @PricePerShare, @TotalValue, @TransactionDate,
                        @FilingDate, @IsDirect, @OwnershipNature, @CreatedAt)
                RETURNING id";

            var result = await db.QuerySingleAsync<Guid>(query, new
            {
                Id = ownershipEvent.Id,
                EntityId = ownershipEvent.EntityId,
                CompanyTicker = ownershipEvent.CompanyTicker,
                CompanyCik = ownershipEvent.CompanyCik,
                FilingId = ToNullable(ownershipEvent.FilingId),
                EventType = ownershipEvent.EventType,
                TransactionType = ToNullableString(ownershipEvent.TransactionType),
                SharesBefore = ToNullable(ownershipEvent.SharesBefore),
                SharesTransacted = ToNullable(ownershipEvent.SharesTransacted),
                SharesAfter = ownershipEvent.SharesAfter,
                PercentOfClass = ToNullable(ownershipEvent.PercentOfClass),
                PricePerShare = ToNullable(ownershipEvent.PricePerShare),
                TotalValue = ToNullable(ownershipEvent.TotalValue),
                TransactionDate = ownershipEvent.TransactionDate,
                FilingDate = ownershipEvent.FilingDate,
                IsDirect = ownershipEvent.IsDirect,
                OwnershipNature = ToNullableString(ownershipEvent.OwnershipNature),
                CreatedAt = ownershipEvent.CreatedAt
            });

            return result;
        }

        public async Task<int> SaveEvents(IEnumerable<OwnershipEvent> events)
        {
            using var db = GetConnection();
            
            var query = @"
                INSERT INTO ownership_events 
                    (id, entity_id, company_ticker, company_cik, filing_id, event_type,
                     transaction_type, shares_before, shares_transacted, shares_after,
                     percent_of_class, price_per_share, total_value, transaction_date,
                     filing_date, is_direct, ownership_nature, created_at)
                VALUES (@Id, @EntityId, @CompanyTicker, @CompanyCik, @FilingId, @EventType,
                        @TransactionType, @SharesBefore, @SharesTransacted, @SharesAfter,
                        @PercentOfClass, @PricePerShare, @TotalValue, @TransactionDate,
                        @FilingDate, @IsDirect, @OwnershipNature, @CreatedAt)";

            var eventsArray = events.ToArray();
            var rowsAffected = await db.ExecuteAsync(query, eventsArray);

            return rowsAffected;
        }

        public async Task<IEnumerable<OwnershipEvent>> GetEventsByCompany(Ticker ticker)
        {
            using var db = GetConnection();
            
            var query = @"
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
                ORDER BY transaction_date DESC";

            var rows = await db.QueryAsync(query, new { Ticker = ticker.Value });
            return rows.Select(MapToOwnershipEvent);
        }

        public async Task<IEnumerable<OwnershipEvent>> GetEventsByCompanyDateRange(Ticker ticker, string startDate, string endDate)
        {
            using var db = GetConnection();
            
            var query = @"
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
                ORDER BY transaction_date DESC";

            var rows = await db.QueryAsync(query, new 
            { 
                Ticker = ticker.Value,
                StartDate = startDate,
                EndDate = endDate
            });
            return rows.Select(MapToOwnershipEvent);
        }

        public async Task<IEnumerable<OwnershipEvent>> GetEventsByEntity(Guid entityId)
        {
            using var db = GetConnection();
            
            var query = @"
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
                ORDER BY transaction_date DESC";

            var rows = await db.QueryAsync(query, new { EntityId = entityId });
            return rows.Select(MapToOwnershipEvent);
        }

        public async Task<FSharpOption<OwnershipEvent>> GetLatestEventForEntityCompany(Guid entityId, Ticker ticker)
        {
            using var db = GetConnection();
            
            var query = @"
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
                LIMIT 1";

            var row = await db.QueryFirstOrDefaultAsync(query, new 
            { 
                EntityId = entityId,
                Ticker = ticker.Value
            });
            
            if (row == null)
                return FSharpOption<OwnershipEvent>.None;
                
            var ownershipEvent = MapToOwnershipEvent(row);
            return FSharpOption<OwnershipEvent>.Some(ownershipEvent);
        }

        // Summary/Analytics

        public async Task<IEnumerable<OwnershipSummary>> GetOwnershipSummary(Ticker ticker)
        {
            using var db = GetConnection();
            
            // This query gets the latest event for each entity that owns shares in the company
            var query = @"
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
                ORDER BY le.shares_after DESC";

            var results = await db.QueryAsync(query, new { Ticker = ticker.Value });
            
            // Convert to OwnershipSummary (need to manually map due to complex structure)
            var summaries = new List<OwnershipSummary>();
            foreach (var row in results)
            {
                var rowDict = (IDictionary<string, object>)row;
                
                // Use the mapper for the entity portion
                var entity = MapToOwnershipEntity(row);

                // Get roles for this entity
                var roles = await GetRolesByEntity(entity.Id);
                var rolesAsFSharpList = Microsoft.FSharp.Collections.ListModule.OfSeq(roles);
                
                var percentOption = rowDict["percent_of_class"] is decimal percentOfClass
                    ? FSharpOption<decimal>.Some(percentOfClass) 
                    : FSharpOption<decimal>.None;

                summaries.Add(new OwnershipSummary(
                    entity,
                    rolesAsFSharpList,
                    (long)rowDict["current_shares"],
                    percentOption,
                    DateTimeOffset.Parse((string)rowDict["last_updated"])
                ));
            }

            return summaries;
        }

        public async Task<IEnumerable<OwnershipEvent>> GetOwnershipTimeline(Ticker ticker, int days)
        {
            using var db = GetConnection();
            
            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-days).ToString("yyyy-MM-dd");
            
            var query = @"
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
                ORDER BY transaction_date DESC";

            var rows = await db.QueryAsync(query, new 
            { 
                Ticker = ticker.Value,
                CutoffDate = cutoffDate
            });
            return rows.Select(MapToOwnershipEvent);
        }
    }
}

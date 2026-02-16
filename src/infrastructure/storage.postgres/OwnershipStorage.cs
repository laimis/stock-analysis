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

        // Entity Management

        public async Task<FSharpOption<OwnershipEntity>> FindEntityByCik(string cik)
        {
            using var db = GetConnection();
            
            var query = @"
                SELECT id as Id, name as Name, entity_type as EntityType, cik as Cik,
                       first_seen as FirstSeen, last_seen as LastSeen, created_at as CreatedAt
                FROM ownership_entities
                WHERE cik = @Cik";

            var result = await db.QueryFirstOrDefaultAsync<OwnershipEntity>(query, new { Cik = cik });
            return result != null ? FSharpOption<OwnershipEntity>.Some(result) : FSharpOption<OwnershipEntity>.None;
        }

        public async Task<IEnumerable<OwnershipEntity>> FindEntitiesByName(string name)
        {
            using var db = GetConnection();
            
            // Use ILIKE for case-insensitive search and % wildcards for fuzzy matching
            var query = @"
                SELECT id as Id, name as Name, entity_type as EntityType, cik as Cik,
                       first_seen as FirstSeen, last_seen as LastSeen, created_at as CreatedAt
                FROM ownership_entities
                WHERE name ILIKE @Name
                ORDER BY name";

            var searchPattern = $"%{name}%";
            var results = await db.QueryAsync<OwnershipEntity>(query, new { Name = searchPattern });
            return results;
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
                Cik = entity.Cik,
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
                SELECT id as Id, entity_id as EntityId, company_ticker as CompanyTicker,
                       company_cik as CompanyCik, relationship_type as RelationshipType,
                       title as Title, is_active as IsActive, first_seen as FirstSeen,
                       last_seen as LastSeen
                FROM ownership_entity_company_roles
                WHERE entity_id = @EntityId
                ORDER BY last_seen DESC";

            var results = await db.QueryAsync<OwnershipEntityCompanyRole>(query, new { EntityId = entityId });
            return results;
        }

        public async Task<IEnumerable<OwnershipEntityCompanyRole>> GetRolesByCompany(Ticker ticker)
        {
            using var db = GetConnection();
            
            var query = @"
                SELECT id as Id, entity_id as EntityId, company_ticker as CompanyTicker,
                       company_cik as CompanyCik, relationship_type as RelationshipType,
                       title as Title, is_active as IsActive, first_seen as FirstSeen,
                       last_seen as LastSeen
                FROM ownership_entity_company_roles
                WHERE company_ticker = @Ticker AND is_active = true
                ORDER BY last_seen DESC";

            var results = await db.QueryAsync<OwnershipEntityCompanyRole>(query, new { Ticker = ticker.Value });
            return results;
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
                Title = role.Title,
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
                FilingId = ownershipEvent.FilingId,
                EventType = ownershipEvent.EventType,
                TransactionType = ownershipEvent.TransactionType,
                SharesBefore = ownershipEvent.SharesBefore,
                SharesTransacted = ownershipEvent.SharesTransacted,
                SharesAfter = ownershipEvent.SharesAfter,
                PercentOfClass = ownershipEvent.PercentOfClass,
                PricePerShare = ownershipEvent.PricePerShare,
                TotalValue = ownershipEvent.TotalValue,
                TransactionDate = ownershipEvent.TransactionDate,
                FilingDate = ownershipEvent.FilingDate,
                IsDirect = ownershipEvent.IsDirect,
                OwnershipNature = ownershipEvent.OwnershipNature,
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
                SELECT id as Id, entity_id as EntityId, company_ticker as CompanyTicker,
                       company_cik as CompanyCik, filing_id as FilingId, event_type as EventType,
                       transaction_type as TransactionType, shares_before as SharesBefore,
                       shares_transacted as SharesTransacted, shares_after as SharesAfter,
                       percent_of_class as PercentOfClass, price_per_share as PricePerShare,
                       total_value as TotalValue, transaction_date as TransactionDate,
                       filing_date as FilingDate, is_direct as IsDirect,
                       ownership_nature as OwnershipNature, created_at as CreatedAt
                FROM ownership_events
                WHERE company_ticker = @Ticker
                ORDER BY transaction_date DESC";

            var results = await db.QueryAsync<OwnershipEvent>(query, new { Ticker = ticker.Value });
            return results;
        }

        public async Task<IEnumerable<OwnershipEvent>> GetEventsByCompanyDateRange(Ticker ticker, string startDate, string endDate)
        {
            using var db = GetConnection();
            
            var query = @"
                SELECT id as Id, entity_id as EntityId, company_ticker as CompanyTicker,
                       company_cik as CompanyCik, filing_id as FilingId, event_type as EventType,
                       transaction_type as TransactionType, shares_before as SharesBefore,
                       shares_transacted as SharesTransacted, shares_after as SharesAfter,
                       percent_of_class as PercentOfClass, price_per_share as PricePerShare,
                       total_value as TotalValue, transaction_date as TransactionDate,
                       filing_date as FilingDate, is_direct as IsDirect,
                       ownership_nature as OwnershipNature, created_at as CreatedAt
                FROM ownership_events
                WHERE company_ticker = @Ticker 
                  AND transaction_date >= @StartDate 
                  AND transaction_date <= @EndDate
                ORDER BY transaction_date DESC";

            var results = await db.QueryAsync<OwnershipEvent>(query, new 
            { 
                Ticker = ticker.Value,
                StartDate = startDate,
                EndDate = endDate
            });
            return results;
        }

        public async Task<IEnumerable<OwnershipEvent>> GetEventsByEntity(Guid entityId)
        {
            using var db = GetConnection();
            
            var query = @"
                SELECT id as Id, entity_id as EntityId, company_ticker as CompanyTicker,
                       company_cik as CompanyCik, filing_id as FilingId, event_type as EventType,
                       transaction_type as TransactionType, shares_before as SharesBefore,
                       shares_transacted as SharesTransacted, shares_after as SharesAfter,
                       percent_of_class as PercentOfClass, price_per_share as PricePerShare,
                       total_value as TotalValue, transaction_date as TransactionDate,
                       filing_date as FilingDate, is_direct as IsDirect,
                       ownership_nature as OwnershipNature, created_at as CreatedAt
                FROM ownership_events
                WHERE entity_id = @EntityId
                ORDER BY transaction_date DESC";

            var results = await db.QueryAsync<OwnershipEvent>(query, new { EntityId = entityId });
            return results;
        }

        public async Task<FSharpOption<OwnershipEvent>> GetLatestEventForEntityCompany(Guid entityId, Ticker ticker)
        {
            using var db = GetConnection();
            
            var query = @"
                SELECT id as Id, entity_id as EntityId, company_ticker as CompanyTicker,
                       company_cik as CompanyCik, filing_id as FilingId, event_type as EventType,
                       transaction_type as TransactionType, shares_before as SharesBefore,
                       shares_transacted as SharesTransacted, shares_after as SharesAfter,
                       percent_of_class as PercentOfClass, price_per_share as PricePerShare,
                       total_value as TotalValue, transaction_date as TransactionDate,
                       filing_date as FilingDate, is_direct as IsDirect,
                       ownership_nature as OwnershipNature, created_at as CreatedAt
                FROM ownership_events
                WHERE entity_id = @EntityId AND company_ticker = @Ticker
                ORDER BY transaction_date DESC
                LIMIT 1";

            var result = await db.QueryFirstOrDefaultAsync<OwnershipEvent>(query, new 
            { 
                EntityId = entityId,
                Ticker = ticker.Value
            });
            return result != null ? FSharpOption<OwnershipEvent>.Some(result) : FSharpOption<OwnershipEvent>.None;
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
                    e.id as Id, e.name as Name, e.entity_type as EntityType, e.cik as Cik,
                    e.first_seen as FirstSeen, e.last_seen as LastSeen, e.created_at as CreatedAt,
                    le.shares_after as CurrentShares,
                    le.percent_of_class as PercentOfClass,
                    le.transaction_date as LastUpdated
                FROM ownership_entities e
                INNER JOIN latest_events le ON e.id = le.entity_id
                WHERE le.shares_after > 0
                ORDER BY le.shares_after DESC";

            var results = await db.QueryAsync<dynamic>(query, new { Ticker = ticker.Value });
            
            // Convert to OwnershipSummary (need to manually map due to complex structure)
            var summaries = new List<OwnershipSummary>();
            foreach (var row in results)
            {
                var entity = new OwnershipEntity
                {
                    Id = row.Id,
                    Name = row.Name,
                    EntityType = row.EntityType,
                    Cik = row.Cik,
                    FirstSeen = row.FirstSeen,
                    LastSeen = row.LastSeen,
                    CreatedAt = row.CreatedAt
                };

                // Get roles for this entity
                var roles = await GetRolesByEntity(entity.Id);
                var rolesAsFSharpList = Microsoft.FSharp.Collections.ListModule.OfSeq(roles);
                
                decimal? percentOfClass = row.PercentOfClass;
                var percentOption = percentOfClass.HasValue 
                    ? FSharpOption<decimal>.Some(percentOfClass.Value) 
                    : FSharpOption<decimal>.None;

                summaries.Add(new OwnershipSummary(
                    entity,
                    rolesAsFSharpList,
                    (long)row.CurrentShares,
                    percentOption,
                    DateTimeOffset.Parse((string)row.LastUpdated)
                ));
            }

            return summaries;
        }

        public async Task<IEnumerable<OwnershipEvent>> GetOwnershipTimeline(Ticker ticker, int days)
        {
            using var db = GetConnection();
            
            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-days).ToString("yyyy-MM-dd");
            
            var query = @"
                SELECT id as Id, entity_id as EntityId, company_ticker as CompanyTicker,
                       company_cik as CompanyCik, filing_id as FilingId, event_type as EventType,
                       transaction_type as TransactionType, shares_before as SharesBefore,
                       shares_transacted as SharesTransacted, shares_after as SharesAfter,
                       percent_of_class as PercentOfClass, price_per_share as PricePerShare,
                       total_value as TotalValue, transaction_date as TransactionDate,
                       filing_date as FilingDate, is_direct as IsDirect,
                       ownership_nature as OwnershipNature, created_at as CreatedAt
                FROM ownership_events
                WHERE company_ticker = @Ticker AND transaction_date >= @CutoffDate
                ORDER BY transaction_date DESC";

            var results = await db.QueryAsync<OwnershipEvent>(query, new 
            { 
                Ticker = ticker.Value,
                CutoffDate = cutoffDate
            });
            return results;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.fs.Adapters.Storage;
using core.Shared;
using Microsoft.FSharp.Core;

namespace storage.memory
{
    public class OwnershipStorage : IOwnershipStorage
    {
        private static readonly Dictionary<Guid, OwnershipEntity> _entitiesById = new();
        private static readonly Dictionary<string, OwnershipEntity> _entitiesByCik = new();
        private static readonly Dictionary<Guid, OwnershipEntityCompanyRole> _rolesById = new();
        private static readonly Dictionary<Guid, OwnershipEvent> _eventsById = new();
        private static readonly object _lock = new();

        // Entity Management

        public Task<FSharpOption<OwnershipEntity>> GetEntityById(Guid entityId)
        {
            lock (_lock)
            {
                _entitiesById.TryGetValue(entityId, out var entity);
                var result = entity != null ? FSharpOption<OwnershipEntity>.Some(entity) : FSharpOption<OwnershipEntity>.None;
                return Task.FromResult(result);
            }
        }

        public Task<FSharpOption<OwnershipEntity>> FindEntityByCik(string cik)
        {
            lock (_lock)
            {
                _entitiesByCik.TryGetValue(cik, out var entity);
                var result = entity != null ? FSharpOption<OwnershipEntity>.Some(entity) : FSharpOption<OwnershipEntity>.None;
                return Task.FromResult(result);
            }
        }

        public Task<IEnumerable<OwnershipEntity>> FindEntitiesByName(string name)
        {
            lock (_lock)
            {
                var results = _entitiesById.Values
                    .Where(e => e.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(e => e.Name)
                    .ToList();
                
                return Task.FromResult<IEnumerable<OwnershipEntity>>(results);
            }
        }

        public Task<Guid> SaveEntity(OwnershipEntity entity)
        {
            lock (_lock)
            {
                // Check if entity with this CIK already exists (upsert logic)
                if (entity.Cik != null && Microsoft.FSharp.Core.OptionModule.IsSome(entity.Cik) &&
                    _entitiesByCik.TryGetValue(Microsoft.FSharp.Core.OptionModule.GetValue(entity.Cik), out var existing))
                {
                    // Update existing entity
                    existing.Name = entity.Name;
                    existing.EntityType = entity.EntityType;
                    existing.LastSeen = entity.LastSeen;
                    return Task.FromResult(existing.Id);
                }
                
                // Insert new entity
                _entitiesById[entity.Id] = entity;
                if (entity.Cik != null && Microsoft.FSharp.Core.OptionModule.IsSome(entity.Cik))
                {
                    _entitiesByCik[Microsoft.FSharp.Core.OptionModule.GetValue(entity.Cik)] = entity;
                }
                
                return Task.FromResult(entity.Id);
            }
        }

        public Task<Unit> UpdateEntityLastSeen(Guid entityId, DateTimeOffset lastSeen)
        {
            lock (_lock)
            {
                if (_entitiesById.TryGetValue(entityId, out var entity))
                {
                    entity.LastSeen = lastSeen;
                }
                return Task.FromResult<Unit>(null);
            }
        }

        // Entity-Company Role Management

        public Task<IEnumerable<OwnershipEntityCompanyRole>> GetRolesByEntity(Guid entityId)
        {
            lock (_lock)
            {
                var results = _rolesById.Values
                    .Where(r => r.EntityId == entityId)
                    .OrderByDescending(r => r.LastSeen)
                    .ToList();
                
                return Task.FromResult<IEnumerable<OwnershipEntityCompanyRole>>(results);
            }
        }

        public Task<IEnumerable<OwnershipEntityCompanyRole>> GetRolesByCompany(Ticker ticker)
        {
            lock (_lock)
            {
                var results = _rolesById.Values
                    .Where(r => r.CompanyTicker.Equals(ticker.Value, StringComparison.OrdinalIgnoreCase) 
                             && r.IsActive)
                    .OrderByDescending(r => r.LastSeen)
                    .ToList();
                
                return Task.FromResult<IEnumerable<OwnershipEntityCompanyRole>>(results);
            }
        }

        public Task<Guid> SaveRole(OwnershipEntityCompanyRole role)
        {
            lock (_lock)
            {
                // Check for existing active role with same entity+company+relationship
                var existingRole = _rolesById.Values.FirstOrDefault(r =>
                    r.EntityId == role.EntityId &&
                    r.CompanyTicker.Equals(role.CompanyTicker, StringComparison.OrdinalIgnoreCase) &&
                    r.RelationshipType.Equals(role.RelationshipType, StringComparison.OrdinalIgnoreCase) &&
                    r.IsActive);

                if (existingRole != null)
                {
                    // Update existing role
                    existingRole.Title = role.Title;
                    existingRole.LastSeen = role.LastSeen;
                    return Task.FromResult(existingRole.Id);
                }

                // Insert new role
                _rolesById[role.Id] = role;
                return Task.FromResult(role.Id);
            }
        }

        public Task<Unit> DeactivateRole(Guid roleId)
        {
            lock (_lock)
            {
                if (_rolesById.TryGetValue(roleId, out var role))
                {
                    role.IsActive = false;
                }
                return Task.FromResult<Unit>(null);
            }
        }

        // Ownership Event Management

        public Task<Guid> SaveEvent(OwnershipEvent ownershipEvent)
        {
            lock (_lock)
            {
                _eventsById[ownershipEvent.Id] = ownershipEvent;
                return Task.FromResult(ownershipEvent.Id);
            }
        }

        public Task<int> SaveEvents(IEnumerable<OwnershipEvent> events)
        {
            lock (_lock)
            {
                var count = 0;
                foreach (var evt in events)
                {
                    _eventsById[evt.Id] = evt;
                    count++;
                }
                return Task.FromResult(count);
            }
        }

        public Task<IEnumerable<OwnershipEvent>> GetEventsByCompany(Ticker ticker)
        {
            lock (_lock)
            {
                var results = _eventsById.Values
                    .Where(e => e.CompanyTicker.Equals(ticker.Value, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(e => e.TransactionDate)
                    .ToList();
                
                return Task.FromResult<IEnumerable<OwnershipEvent>>(results);
            }
        }

        public Task<IEnumerable<OwnershipEvent>> GetEventsByCompanyDateRange(Ticker ticker, string startDate, string endDate)
        {
            lock (_lock)
            {
                var results = _eventsById.Values
                    .Where(e => e.CompanyTicker.Equals(ticker.Value, StringComparison.OrdinalIgnoreCase)
                             && string.Compare(e.TransactionDate, startDate, StringComparison.Ordinal) >= 0
                             && string.Compare(e.TransactionDate, endDate, StringComparison.Ordinal) <= 0)
                    .OrderByDescending(e => e.TransactionDate)
                    .ToList();
                
                return Task.FromResult<IEnumerable<OwnershipEvent>>(results);
            }
        }

        public Task<IEnumerable<OwnershipEvent>> GetEventsByEntity(Guid entityId)
        {
            lock (_lock)
            {
                var results = _eventsById.Values
                    .Where(e => e.EntityId == entityId)
                    .OrderByDescending(e => e.TransactionDate)
                    .ToList();
                
                return Task.FromResult<IEnumerable<OwnershipEvent>>(results);
            }
        }

        public Task<FSharpOption<OwnershipEvent>> GetLatestEventForEntityCompany(Guid entityId, Ticker ticker)
        {
            lock (_lock)
            {
                var result = _eventsById.Values
                    .Where(e => e.EntityId == entityId 
                             && e.CompanyTicker.Equals(ticker.Value, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(e => e.TransactionDate)
                    .FirstOrDefault();
                
                var option = result != null ? FSharpOption<OwnershipEvent>.Some(result) : FSharpOption<OwnershipEvent>.None;
                return Task.FromResult(option);
            }
        }

        // Summary/Analytics

        public Task<IEnumerable<OwnershipSummary>> GetOwnershipSummary(Ticker ticker)
        {
            lock (_lock)
            {
                // Get latest event for each entity
                var latestEventsByEntity = _eventsById.Values
                    .Where(e => e.CompanyTicker.Equals(ticker.Value, StringComparison.OrdinalIgnoreCase))
                    .GroupBy(e => e.EntityId)
                    .Select(g => g.OrderByDescending(e => e.TransactionDate).First())
                    .Where(e => e.SharesAfter > 0)
                    .ToList();

                var summaries = new List<OwnershipSummary>();
                foreach (var evt in latestEventsByEntity)
                {
                    if (_entitiesById.TryGetValue(evt.EntityId, out var entity))
                    {
                        var roles = _rolesById.Values
                            .Where(r => r.EntityId == entity.Id)
                            .ToList();

                        var rolesAsFSharpList = Microsoft.FSharp.Collections.ListModule.OfSeq(roles);

                        summaries.Add(new OwnershipSummary(
                            entity,
                            rolesAsFSharpList,
                            evt.SharesAfter,
                            evt.PercentOfClass,
                            DateTimeOffset.Parse(evt.TransactionDate)
                        ));
                    }
                }

                var orderedResults = summaries.OrderByDescending(s => s.CurrentShares).ToList();
                return Task.FromResult<IEnumerable<OwnershipSummary>>(orderedResults);
            }
        }

        public Task<IEnumerable<OwnershipEvent>> GetOwnershipTimeline(Ticker ticker, int days)
        {
            lock (_lock)
            {
                var cutoffDate = DateTimeOffset.UtcNow.AddDays(-days).ToString("yyyy-MM-dd");
                
                var results = _eventsById.Values
                    .Where(e => e.CompanyTicker.Equals(ticker.Value, StringComparison.OrdinalIgnoreCase)
                             && string.Compare(e.TransactionDate, cutoffDate, StringComparison.Ordinal) >= 0)
                    .OrderByDescending(e => e.TransactionDate)
                    .ToList();
                
                return Task.FromResult<IEnumerable<OwnershipEvent>>(results);
            }
        }
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using core.fs.Adapters.Storage;
using core.Shared;
using Microsoft.FSharp.Core;
using Xunit;

namespace storagetests
{
    public abstract class OwnershipStorageTests
    {
        protected abstract IOwnershipStorage GetStorage();

        // Helper method to create Some option
        private static FSharpOption<T> Some<T>(T value) => FSharpOption<T>.Some(value);

        private static OwnershipEntity CreateEntity(string name, string entityType, FSharpOption<string> cik)
        {
            return new OwnershipEntity
            {
                Id = Guid.NewGuid(),
                Name = name,
                EntityType = entityType,
                Cik = cik,
                FirstSeen = DateTimeOffset.UtcNow,
                LastSeen = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        private static OwnershipEntityCompanyRole CreateRole(Guid entityId, string ticker, string cik, string relationshipType, FSharpOption<string> title)
        {
            return new OwnershipEntityCompanyRole
            {
                Id = Guid.NewGuid(),
                EntityId = entityId,
                CompanyTicker = ticker,
                CompanyCik = cik,
                RelationshipType = relationshipType,
                Title = title,
                IsActive = true,
                FirstSeen = DateTimeOffset.UtcNow,
                LastSeen = DateTimeOffset.UtcNow
            };
        }

        private static OwnershipEvent CreateEvent(Guid entityId, string ticker, string cik, FSharpOption<Guid> filingId, 
            string eventType, FSharpOption<string> transactionType, FSharpOption<long> sharesBefore, FSharpOption<long> sharesTransacted,
            long sharesAfter, FSharpOption<decimal> percentOfClass, FSharpOption<decimal> pricePerShare, FSharpOption<decimal> totalValue,
            string transactionDate, string filingDate, bool isDirect, FSharpOption<string> ownershipNature)
        {
            return new OwnershipEvent
            {
                Id = Guid.NewGuid(),
                EntityId = entityId,
                CompanyTicker = ticker,
                CompanyCik = cik,
                FilingId = filingId,
                EventType = eventType,
                TransactionType = transactionType,
                SharesBefore = sharesBefore,
                SharesTransacted = sharesTransacted,
                SharesAfter = sharesAfter,
                PercentOfClass = percentOfClass,
                PricePerShare = pricePerShare,
                TotalValue = totalValue,
                TransactionDate = transactionDate,
                FilingDate = filingDate,
                IsDirect = isDirect,
                OwnershipNature = ownershipNature,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        [Fact]
        public async Task SaveAndRetrieveEntityById()
        {
            var storage = GetStorage();
            var entity = CreateEntity("Warren Buffett", "individual", Some("0000315066"));

            await storage.SaveEntity(entity);

            var fromDbOption = await storage.GetEntityById(entity.Id);

            Assert.True(FSharpOption<OwnershipEntity>.get_IsSome(fromDbOption));
            
            var fromDb = fromDbOption.Value;
            Assert.Equal(entity.Id, fromDb.Id);
            Assert.Equal(entity.Name, fromDb.Name);
            Assert.Equal(entity.EntityType, fromDb.EntityType);
            Assert.Equal(entity.Cik, fromDb.Cik);
        }

        [Fact]
        public async Task FindEntityByCik()
        {
            var storage = GetStorage();
            var cik = "0001234567";
            var entity = CreateEntity("Test Institution", "institution", Some(cik));

            await storage.SaveEntity(entity);

            var fromDbOption = await storage.FindEntityByCik(cik);

            Assert.True(FSharpOption<OwnershipEntity>.get_IsSome(fromDbOption));
            Assert.Equal(entity.Name, fromDbOption.Value.Name);
        }

        [Fact]
        public async Task EntityWithNullCikHandling()
        {
            var storage = GetStorage();
            var entity = CreateEntity("Unknown Individual", "individual", FSharpOption<string>.None);

            await storage.SaveEntity(entity);

            var fromDbOption = await storage.GetEntityById(entity.Id);

            Assert.True(FSharpOption<OwnershipEntity>.get_IsSome(fromDbOption));
            Assert.True(FSharpOption<string>.get_IsNone(fromDbOption.Value.Cik));
        }

        [Fact]
        public async Task FindEntitiesByName()
        {
            var storage = GetStorage();
            var uniqueName = $"Vanguard Group {Guid.NewGuid()}";
            var entity = CreateEntity(uniqueName, "institution", Some("0001234567"));

            await storage.SaveEntity(entity);

            // Search with partial name (fuzzy match)
            var results = await storage.FindEntitiesByName("Vanguard");

            Assert.Contains(results, e => e.Name == uniqueName);
        }

        [Fact]
        public async Task UpdateEntityLastSeen()
        {
            var storage = GetStorage();
            var entity = CreateEntity("Test Entity", "individual", Some("9999999999"));

            await storage.SaveEntity(entity);

            var newLastSeen = DateTimeOffset.UtcNow.AddDays(1);
            await storage.UpdateEntityLastSeen(entity.Id, newLastSeen);

            var fromDbOption = await storage.GetEntityById(entity.Id);

            Assert.True(FSharpOption<OwnershipEntity>.get_IsSome(fromDbOption));
            Assert.Equal(newLastSeen.DateTime, fromDbOption.Value.LastSeen.DateTime, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task SaveAndRetrieveEntityCompanyRole()
        {
            var storage = GetStorage();
            var entity = CreateEntity("Jane Doe", "individual", Some("1111111111"));
            await storage.SaveEntity(entity);

            var role = CreateRole(entity.Id, "AAPL", "0000320193", "officer", Some("CFO"));

            await storage.SaveRole(role);

            var roles = await storage.GetRolesByEntity(entity.Id);

            Assert.Single(roles);
            var fromDb = roles.First();
            Assert.Equal(role.Id, fromDb.Id);
            Assert.Equal(role.EntityId, fromDb.EntityId);
            Assert.Equal(role.CompanyTicker, fromDb.CompanyTicker);
            Assert.Equal(role.RelationshipType, fromDb.RelationshipType);
            Assert.True(FSharpOption<string>.get_IsSome(fromDb.Title));
            Assert.Equal("CFO", fromDb.Title.Value);
            Assert.True(fromDb.IsActive);
        }

        [Fact]
        public async Task RoleWithNullTitleHandling()
        {
            var storage = GetStorage();
            var entity = CreateEntity("John Smith", "individual", Some("2222222222"));
            await storage.SaveEntity(entity);

            var role = CreateRole(entity.Id, "TSLA", "0001318605", "beneficial_owner", FSharpOption<string>.None);

            await storage.SaveRole(role);

            var roles = await storage.GetRolesByEntity(entity.Id);

            Assert.Single(roles);
            Assert.True(FSharpOption<string>.get_IsNone(roles.First().Title));
        }

        [Fact]
        public async Task GetRolesByCompany()
        {
            var storage = GetStorage();
            var ticker = new Ticker($"TEST{Guid.NewGuid().ToString().Substring(0, 4)}");
            
            var entity1 = CreateEntity("Director 1", "individual", Some("3333333333"));
            var entity2 = CreateEntity("Director 2", "individual", Some("4444444444"));
            await storage.SaveEntity(entity1);
            await storage.SaveEntity(entity2);

            var role1 = CreateRole(entity1.Id, ticker.Value, "1234567890", "director", Some("Board Member"));
            var role2 = CreateRole(entity2.Id, ticker.Value, "1234567890", "director", Some("Chairman"));
            await storage.SaveRole(role1);
            await storage.SaveRole(role2);

            var roles = await storage.GetRolesByCompany(ticker);

            Assert.Equal(2, roles.Count());
        }

        [Fact]
        public async Task DeactivateRole()
        {
            var storage = GetStorage();
            var entity = CreateEntity("Former CEO", "individual", Some("5555555555"));
            await storage.SaveEntity(entity);

            var role = CreateRole(entity.Id, "NVDA", "0001045810", "officer", Some("CEO"));
            await storage.SaveRole(role);

            await storage.DeactivateRole(role.Id);

            var roles = await storage.GetRolesByEntity(entity.Id);
            Assert.Single(roles);
            Assert.False(roles.First().IsActive);
        }

        [Fact]
        public async Task SaveAndRetrieveOwnershipEvent()
        {
            var storage = GetStorage();
            var entity = CreateEntity("Insider Trader", "individual", Some("6666666666"));
            await storage.SaveEntity(entity);

            var ownershipEvent = CreateEvent(entity.Id, "MSFT", "0000789019", Some(Guid.NewGuid()), "transaction",
                Some("purchase"), Some(10000L), Some(5000L), 15000L, Some(0.01m), Some(150.50m), Some(752500m),
                "2024-01-15", "2024-01-16", true, Some("sole voting power"));

            await storage.SaveEvent(ownershipEvent);

            var events = await storage.GetEventsByEntity(entity.Id);

            Assert.Single(events);
            var fromDb = events.First();
            Assert.Equal(ownershipEvent.Id, fromDb.Id);
            Assert.Equal(ownershipEvent.EntityId, fromDb.EntityId);
            Assert.Equal(ownershipEvent.CompanyTicker, fromDb.CompanyTicker);
            Assert.Equal(ownershipEvent.EventType, fromDb.EventType);
            Assert.True(FSharpOption<string>.get_IsSome(fromDb.TransactionType));
            Assert.Equal("purchase", fromDb.TransactionType.Value);
            Assert.True(FSharpOption<long>.get_IsSome(fromDb.SharesBefore));
            Assert.Equal(10000L, fromDb.SharesBefore.Value);
            Assert.True(FSharpOption<long>.get_IsSome(fromDb.SharesTransacted));
            Assert.Equal(5000L, fromDb.SharesTransacted.Value);
            Assert.Equal(15000L, fromDb.SharesAfter);
            Assert.True(FSharpOption<decimal>.get_IsSome(fromDb.PercentOfClass));
            Assert.Equal(0.01m, fromDb.PercentOfClass.Value);
            Assert.True(FSharpOption<decimal>.get_IsSome(fromDb.PricePerShare));
            Assert.Equal(150.50m, fromDb.PricePerShare.Value);
            Assert.True(FSharpOption<decimal>.get_IsSome(fromDb.TotalValue));
            Assert.Equal(752500m, fromDb.TotalValue.Value);
        }

        [Fact]
        public async Task OwnershipEventWithNullableFieldsHandling()
        {
            var storage = GetStorage();
            var entity = CreateEntity("Simplified Reporter", "individual", Some("7777777777"));
            await storage.SaveEntity(entity);

            var ownershipEvent = CreateEvent(entity.Id, "GOOG", "0001652044", FSharpOption<Guid>.None, "position_disclosure",
                FSharpOption<string>.None, FSharpOption<long>.None, FSharpOption<long>.None, 100000L,
                FSharpOption<decimal>.None, FSharpOption<decimal>.None, FSharpOption<decimal>.None,
                "2024-03-01", "2024-03-02", false, FSharpOption<string>.None);

            await storage.SaveEvent(ownershipEvent);

            var events = await storage.GetEventsByEntity(entity.Id);

            Assert.Single(events);
            var fromDb = events.First();
            Assert.True(FSharpOption<Guid>.get_IsNone(fromDb.FilingId));
            Assert.True(FSharpOption<string>.get_IsNone(fromDb.TransactionType));
            Assert.True(FSharpOption<long>.get_IsNone(fromDb.SharesBefore));
            Assert.True(FSharpOption<long>.get_IsNone(fromDb.SharesTransacted));
            Assert.True(FSharpOption<decimal>.get_IsNone(fromDb.PercentOfClass));
            Assert.True(FSharpOption<decimal>.get_IsNone(fromDb.PricePerShare));
            Assert.True(FSharpOption<decimal>.get_IsNone(fromDb.TotalValue));
            Assert.True(FSharpOption<string>.get_IsNone(fromDb.OwnershipNature));
        }

        [Fact]
        public async Task SaveMultipleEvents()
        {
            var storage = GetStorage();
            var entity = CreateEntity("Bulk Trader", "individual", Some("8888888888"));
            await storage.SaveEntity(entity);

            var events = Enumerable.Range(0, 5).Select(i =>
                CreateEvent(entity.Id, "AMZN", "0001018724", FSharpOption<Guid>.None, "transaction",
                    Some("purchase"), Some((long)(1000 * i)), Some(1000L), (long)(1000 * (i + 1)),
                    FSharpOption<decimal>.None, Some(100m + i), FSharpOption<decimal>.None,
                    $"2024-0{i + 1}-15", $"2024-0{i + 1}-16", true, FSharpOption<string>.None)
            ).ToArray();

            var count = await storage.SaveEvents(events);

            Assert.Equal(5, count);

            var savedEvents = await storage.GetEventsByEntity(entity.Id);
            Assert.Equal(5, savedEvents.Count());
        }

        [Fact]
        public async Task GetEventsByCompany()
        {
            var storage = GetStorage();
            var ticker = new Ticker($"EVT{Guid.NewGuid().ToString().Substring(0, 4)}");
            
            var entity1 = CreateEntity("Trader A", "individual", Some("9999990001"));
            var entity2 = CreateEntity("Trader B", "individual", Some("9999990002"));
            await storage.SaveEntity(entity1);
            await storage.SaveEntity(entity2);

            var event1 = CreateEvent(entity1.Id, ticker.Value, "1111111111", FSharpOption<Guid>.None, "transaction",
                Some("purchase"), FSharpOption<long>.None, Some(1000L), 1000L, FSharpOption<decimal>.None,
                Some(50m), FSharpOption<decimal>.None, "2024-01-10", "2024-01-11", true, FSharpOption<string>.None);
            var event2 = CreateEvent(entity2.Id, ticker.Value, "1111111111", FSharpOption<Guid>.None, "transaction",
                Some("sale"), Some(2000L), Some(500L), 1500L, FSharpOption<decimal>.None,
                Some(55m), FSharpOption<decimal>.None, "2024-01-12", "2024-01-13", true, FSharpOption<string>.None);
            
            await storage.SaveEvent(event1);
            await storage.SaveEvent(event2);

            var events = await storage.GetEventsByCompany(ticker);

            Assert.Equal(2, events.Count());
        }

        [Fact]
        public async Task GetEventsByCompanyDateRange()
        {
            var storage = GetStorage();
            var ticker = new Ticker($"RNG{Guid.NewGuid().ToString().Substring(0, 4)}");
            var entity = CreateEntity("Date Range Trader", "individual", Some("9999990003"));
            await storage.SaveEntity(entity);

            // Create events across different dates
            var event1 = CreateEvent(entity.Id, ticker.Value, "2222222222", FSharpOption<Guid>.None, "transaction",
                Some("purchase"), FSharpOption<long>.None, Some(100L), 100L, FSharpOption<decimal>.None,
                Some(10m), FSharpOption<decimal>.None, "2024-01-05", "2024-01-06", true, FSharpOption<string>.None);
            var event2 = CreateEvent(entity.Id, ticker.Value, "2222222222", FSharpOption<Guid>.None, "transaction",
                Some("purchase"), Some(100L), Some(100L), 200L, FSharpOption<decimal>.None,
                Some(11m), FSharpOption<decimal>.None, "2024-01-15", "2024-01-16", true, FSharpOption<string>.None);
            var event3 = CreateEvent(entity.Id, ticker.Value, "2222222222", FSharpOption<Guid>.None, "transaction",
                Some("purchase"), Some(200L), Some(100L), 300L, FSharpOption<decimal>.None,
                Some(12m), FSharpOption<decimal>.None, "2024-01-25", "2024-01-26", true, FSharpOption<string>.None);
            
            await storage.SaveEvent(event1);
            await storage.SaveEvent(event2);
            await storage.SaveEvent(event3);

            // Query for events in the middle range
            var events = await storage.GetEventsByCompanyDateRange(ticker, "2024-01-10", "2024-01-20");

            Assert.Single(events);
            Assert.Equal(event2.Id, events.First().Id);
        }

        [Fact]
        public async Task GetLatestEventForEntityCompany()
        {
            var storage = GetStorage();
            var ticker = new Ticker($"LAT{Guid.NewGuid().ToString().Substring(0, 4)}");
            var entity = CreateEntity("Latest Tracker", "individual", Some("9999990004"));
            await storage.SaveEntity(entity);

            var event1 = CreateEvent(entity.Id, ticker.Value, "3333333333", FSharpOption<Guid>.None, "transaction",
                Some("purchase"), FSharpOption<long>.None, Some(100L), 100L, FSharpOption<decimal>.None,
                Some(10m), FSharpOption<decimal>.None, "2024-01-10", "2024-01-11", true, FSharpOption<string>.None);
            var event2 = CreateEvent(entity.Id, ticker.Value, "3333333333", FSharpOption<Guid>.None, "transaction",
                Some("purchase"), Some(100L), Some(50L), 150L, FSharpOption<decimal>.None,
                Some(11m), FSharpOption<decimal>.None, "2024-01-20", "2024-01-21", true, FSharpOption<string>.None);
            
            await storage.SaveEvent(event1);
            await storage.SaveEvent(event2);

            var latestOption = await storage.GetLatestEventForEntityCompany(entity.Id, ticker);

            Assert.True(FSharpOption<OwnershipEvent>.get_IsSome(latestOption));
            Assert.Equal(event2.Id, latestOption.Value.Id);
            Assert.Equal(150L, latestOption.Value.SharesAfter);
        }

        [Fact]
        public async Task GetOwnershipSummary()
        {
            var storage = GetStorage();
            var ticker = new Ticker($"SUM{Guid.NewGuid().ToString().Substring(0, 4)}");
            
            var entity = CreateEntity("Summary Entity", "institution", Some("9999990005"));
            await storage.SaveEntity(entity);

            var role = CreateRole(entity.Id, ticker.Value, "4444444444", "institutional_holder", Some("Fund Manager"));
            await storage.SaveRole(role);

            var ownershipEvent = CreateEvent(entity.Id, ticker.Value, "4444444444", FSharpOption<Guid>.None, "position_disclosure",
                FSharpOption<string>.None, FSharpOption<long>.None, FSharpOption<long>.None, 50000L,
                Some(5.5m), FSharpOption<decimal>.None, FSharpOption<decimal>.None, "2024-06-30", "2024-07-01", true, FSharpOption<string>.None);
            await storage.SaveEvent(ownershipEvent);

            var summary = await storage.GetOwnershipSummary(ticker);

            Assert.Single(summary);
            var item = summary.First();
            Assert.Equal(entity.Name, item.Entity.Name);
            Assert.Single(item.Roles);
            Assert.Equal("institutional_holder", item.Roles.First().RelationshipType);
            Assert.Equal(50000L, item.CurrentShares);
            Assert.True(FSharpOption<decimal>.get_IsSome(item.PercentOfClass));
            Assert.Equal(5.5m, item.PercentOfClass.Value);
        }

        [Fact]
        public async Task GetOwnershipTimeline()
        {
            var storage = GetStorage();
            var ticker = new Ticker($"TML{Guid.NewGuid().ToString().Substring(0, 4)}");
            var entity = CreateEntity("Timeline Entity", "individual", Some("9999990006"));
            await storage.SaveEntity(entity);

            var today = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
            var yesterday = DateTimeOffset.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
            var weekAgo = DateTimeOffset.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");
            var monthAgo = DateTimeOffset.UtcNow.AddDays(-31).ToString("yyyy-MM-dd");

            var event1 = CreateEvent(entity.Id, ticker.Value, "5555555555", FSharpOption<Guid>.None, "transaction",
                Some("purchase"), FSharpOption<long>.None, Some(100L), 100L, FSharpOption<decimal>.None,
                Some(10m), FSharpOption<decimal>.None, monthAgo, monthAgo, true, FSharpOption<string>.None);
            var event2 = CreateEvent(entity.Id, ticker.Value, "5555555555", FSharpOption<Guid>.None, "transaction",
                Some("purchase"), Some(100L), Some(100L), 200L, FSharpOption<decimal>.None,
                Some(11m), FSharpOption<decimal>.None, weekAgo, weekAgo, true, FSharpOption<string>.None);
            var event3 = CreateEvent(entity.Id, ticker.Value, "5555555555", FSharpOption<Guid>.None, "transaction",
                Some("sale"), Some(200L), Some(50L), 150L, FSharpOption<decimal>.None,
                Some(12m), FSharpOption<decimal>.None, yesterday, yesterday, true, FSharpOption<string>.None);
            
            await storage.SaveEvent(event1);
            await storage.SaveEvent(event2);
            await storage.SaveEvent(event3);

            // Get events from last 30 days
            var timeline = await storage.GetOwnershipTimeline(ticker, 30);

            // Should get events from last 30 days (not the one from 31 days ago)
            Assert.Equal(2, timeline.Count());
            Assert.Contains(timeline, e => e.Id == event2.Id);
            Assert.Contains(timeline, e => e.Id == event3.Id);
        }

        [Fact]
        public async Task EntityUpsertBehavior()
        {
            var storage = GetStorage();
            var cik = "0001111111";
            var entity = CreateEntity("Original Name", "individual", Some(cik));

            // First save
            var id1 = await storage.SaveEntity(entity);

            // Update some fields and save again with same ID
            var updatedEntity = new OwnershipEntity
            {
                Id = entity.Id,
                Name = "Updated Name",
                EntityType = entity.EntityType,
                Cik = entity.Cik,
                FirstSeen = entity.FirstSeen,
                LastSeen = DateTimeOffset.UtcNow,
                CreatedAt = entity.CreatedAt
            };

            var id2 = await storage.SaveEntity(updatedEntity);

            Assert.Equal(id1, id2);

            var fromDbOption = await storage.GetEntityById(entity.Id);
            Assert.True(FSharpOption<OwnershipEntity>.get_IsSome(fromDbOption));
            Assert.Equal("Updated Name", fromDbOption.Value.Name);
        }
    }
}

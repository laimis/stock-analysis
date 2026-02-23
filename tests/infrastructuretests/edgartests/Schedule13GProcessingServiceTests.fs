namespace edgartests

open System
open Xunit
open secedgar.fs
open core.fs.Adapters.Storage
open core.Shared
open Microsoft.Extensions.Logging

module Schedule13GProcessingServiceTests =
    
    // Mock implementations for testing
    type MockSECFilingStorage() =
        interface ISECFilingStorage with
            member _.SaveFiling(_) = task { return true }
            member _.SaveFilings(_) = task { return 0 }
            member _.GetFilingsByTicker(_) = task { return Seq.empty }
            member _.GetRecentFilingsByTicker(_) (_) = task { return Seq.empty }
            member _.GetFilingsByTickers(_) (_) = task { return Seq.empty }
            member _.FilingExists(_) = task { return false }
            member _.GetFilingsByFormType(formTypes) (limit) = 
                task {
                    // Return a mock SC 13G filing
                    if Seq.contains "SC 13G" formTypes then
                        let mockFiling = {
                            Id = Guid.NewGuid()
                            Ticker = "DOCS"
                            Cik = "0001516513"
                            FormType = "SC 13G"
                            FilingDate = "2026-01-31"
                            ReportDate = Some "2026-01-31"
                            Description = "SC 13G"
                            FilingUrl = "https://www.sec.gov/Archives/edgar/data/1516513/000031506626000439/0000315066-26-000439-index.html"
                            DocumentUrl = "https://www.sec.gov/Archives/edgar/data/1516513/000031506626000439/primary_doc.xml"
                            CreatedAt = DateTimeOffset.UtcNow
                            IsXBRL = false
                            IsInlineXBRL = false
                        }
                        return [mockFiling] |> Seq.ofList
                    else
                        return Seq.empty
                }            
            member _.GetFilingsWithoutOwnershipEvents(_) = task { return Seq.empty }
            member this.GetFilingsSince(ticker: Ticker) (since: DateTimeOffset): Threading.Tasks.Task<Collections.Generic.IEnumerable<SECFilingRecord>> = 
                failwith "Not Implemented"
            member this.GetWatermark(userId: string) (ticker: Ticker): Threading.Tasks.Task<DateTimeOffset option> = 
                failwith "Not Implemented"
            member this.UpsertWatermark(userId: string) (ticker: Ticker) (timestamp: DateTimeOffset): Threading.Tasks.Task<unit> = 
                failwith "Not Implemented"
    
    type MockOwnershipStorage() =
        let mutable entities = Map.empty<string, OwnershipEntity>
        let mutable events = []
        
        member this.GetStoredEvents() = events
        
        interface IOwnershipStorage with
            member _.GetEntityById(_) = task { return None }
            member _.GetEntitiesByIds(_) = task { return Seq.empty }
            member _.FindEntityByCik(cik) = 
                task {
                    return entities |> Map.tryFind cik
                }
            member _.FindEntitiesByName(_) = task { return Seq.empty }
            member _.SaveEntity(entity) = 
                task {
                    entities <- entities |> Map.add (entity.Cik |> Option.defaultValue "") entity
                    return entity.Id
                }
            member _.UpdateEntityLastSeen(_) (_) = task { return () }
            member _.GetRolesByEntity(_) = task { return Seq.empty }
            member _.GetRolesByCompany(_) = task { return Seq.empty }
            member _.SaveRole(_) = task { return Guid.NewGuid() }
            member _.DeactivateRole(_) = task { return () }
            member _.SaveEvent(event) = 
                task {
                    events <- event :: events
                    return event.Id
                }
            member _.SaveEvents(_) = task { return 0 }
            member this.GetEventsByCompany(_) = 
                task {
                    return events |> List.toSeq
                }
            member _.GetEventsByFilingId(_) = task { return Seq.empty }
            member _.GetEventsByCompanyDateRange(_) (_) (_) = task { return Seq.empty }
            member _.GetEventsByEntity(_) = task { return Seq.empty }
            member _.GetLatestEventForEntityCompany(_) (_) = task { return None }
            member _.GetOwnershipSummary(_) = task { return Seq.empty }
            member _.GetOwnershipTimeline(_) (_) = task { return Seq.empty }
            member _.GetRecentTimelines(_) = task { return Seq.empty }
    
    [<Fact>]
    [<Trait("Category", "Integration")>]
    let ``Service processes Schedule 13G filing and creates ownership event`` () = task {
        // Arrange
        let mockFilingStorage = MockSECFilingStorage()
        let mockOwnershipStorage = MockOwnershipStorage()
        
        // Use the real EdgarClient to fetch the XML
        let accountStorage = new storage.postgres.AccountStorage(
            new testutils.FakeOutbox(),
            testutils.CredsHelper.GetDbCreds()
        )
        let secClient = new EdgarClient(None, Some accountStorage)
        
        let loggerFactory = LoggerFactory.Create(fun builder -> builder.SetMinimumLevel(LogLevel.Information) |> ignore)
        let logger = loggerFactory.CreateLogger<Schedule13GProcessingService>()
        
        let service = Schedule13GProcessingService(
            mockFilingStorage,
            mockOwnershipStorage,
            secClient,
            logger
        )
        
        // Act
        do! service.Execute()
        
        // Assert - check that ownership event was created
        let! events = (mockOwnershipStorage :> IOwnershipStorage).GetEventsByCompany (Ticker "DOCS")
        Assert.NotEmpty(events)
        
        let event = events |> Seq.head
        Assert.Equal("DOCS", event.CompanyTicker)
        Assert.True(event.SharesAfter |> Option.exists (fun s -> s > 0L), "Expected shares after to be greater than 0")
    }
    
    [<Fact>]
    [<Trait("Category", "Integration")>]
    let ``Service does not reprocess already processed filings`` () = task {
        // Arrange
        let mockFilingStorage = MockSECFilingStorage()
        let mockOwnershipStorage = MockOwnershipStorage()
        
        let accountStorage = new storage.postgres.AccountStorage(
            new testutils.FakeOutbox(),
            testutils.CredsHelper.GetDbCreds()
        )
        let secClient = new EdgarClient(None, Some accountStorage)
        
        let loggerFactory = LoggerFactory.Create(fun builder -> builder.SetMinimumLevel(LogLevel.Information) |> ignore)
        let logger = loggerFactory.CreateLogger<Schedule13GProcessingService>()
        
        let service = Schedule13GProcessingService(
            mockFilingStorage,
            mockOwnershipStorage,
            secClient,
            logger
        )
        
        // Act - run twice
        do! service.Execute()
        do! service.Execute()
        
        // Assert - should only have one event (not duplicated)
        let! events = (mockOwnershipStorage :> IOwnershipStorage).GetEventsByCompany (Ticker "DOCS")
        let eventCount = events |> Seq.length
        Assert.Equal(1, eventCount)
    }
    
    [<Fact>]
    [<Trait("Category", "Integration")>]
    let ``Service creates entity if not found`` () = task {
        // Arrange
        let mockFilingStorage = MockSECFilingStorage()
        let mockOwnershipStorage = MockOwnershipStorage()
        
        let accountStorage = new storage.postgres.AccountStorage(
            new testutils.FakeOutbox(),
            testutils.CredsHelper.GetDbCreds()
        )
        let secClient = new EdgarClient(None, Some accountStorage)
        
        let loggerFactory = LoggerFactory.Create(fun builder -> builder.SetMinimumLevel(LogLevel.Information) |> ignore)
        let logger = loggerFactory.CreateLogger<Schedule13GProcessingService>()
        
        let service = Schedule13GProcessingService(
            mockFilingStorage,
            mockOwnershipStorage,
            secClient,
            logger
        )
        
        // Act
        do! service.Execute()
        
        // Assert - verify entity was created
        let! events = (mockOwnershipStorage :> IOwnershipStorage).GetEventsByCompany (Ticker "DOCS")
        Assert.NotEmpty(events)
        
        let event = events |> Seq.head
        Assert.NotEqual(Guid.Empty, event.EntityId)
    }
    
    [<Fact>]
    [<Trait("Category", "Integration")>]
    let ``End-to-end test with real database`` () = task {
        // Arrange - use REAL storage implementations
        let dbCreds = testutils.CredsHelper.GetDbCreds()
        let realFilingStorage = new storage.postgres.SECFilingStorage(dbCreds)
        let realOwnershipStorage = new storage.postgres.OwnershipStorage(dbCreds)
        
        let accountStorage = new storage.postgres.AccountStorage(
            new testutils.FakeOutbox(),
            dbCreds
        )
        let secClient = new EdgarClient(None, Some accountStorage)
        
        let loggerFactory = LoggerFactory.Create(fun builder -> 
            builder.SetMinimumLevel(LogLevel.Information) |> ignore
        )
        let logger = loggerFactory.CreateLogger<Schedule13GProcessingService>()
        
        let service = Schedule13GProcessingService(
            realFilingStorage,
            realOwnershipStorage,
            secClient,
            logger
        )
        
        // Act
        logger.LogInformation("=== STARTING END-TO-END TEST WITH REAL DATABASE ===")
        do! service.Execute()
        logger.LogInformation("=== SERVICE EXECUTION COMPLETED ===")
        
        // Assert - This will process whatever 13G filings exist in your database
        // Check the logs to see what was processed
        // Note: This test intentionally has no hard assertions since it depends on your actual database state
    }

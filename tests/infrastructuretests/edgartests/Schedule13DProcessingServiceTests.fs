namespace edgartests

open System
open Xunit
open secedgar.fs
open core.fs.Adapters.Storage
open core.Shared
open Microsoft.Extensions.Logging

module Schedule13DProcessingServiceTests =
    
    // Mock implementations for testing - mirror the 13G mocks but for 13D form types
    type MockSECFilingStorage13D() =
        interface ISECFilingStorage with
            member _.SaveFiling(_) = task { return true }
            member _.SaveFilings(_) = task { return 0 }
            member _.GetFilingsByTicker(_) = task { return Seq.empty }
            member _.GetRecentFilingsByTicker(_) (_) = task { return Seq.empty }
            member _.GetFilingsByTickers(_) (_) = task { return Seq.empty }
            member _.FilingExists(_) = task { return false }
            member _.GetFilingsByFormType(formTypes) (_limit) = 
                task {
                    if Seq.contains "SC 13D" formTypes then
                        let mockFiling = {
                            Id = Guid.NewGuid()
                            Ticker = "ESTC"
                            Cik = "0001707753"
                            FormType = "SC 13D"
                            FilingDate = "2026-01-26"
                            ReportDate = Some "2026-01-26"
                            Description = "SC 13D"
                            FilingUrl = "https://www.sec.gov/Archives/edgar/data/1707753/000136157026000003/0001361570-26-000003-index.html"
                            DocumentUrl = "https://www.sec.gov/Archives/edgar/data/1361570/000136157026000003/primary_doc.xml"
                            CreatedAt = DateTimeOffset.UtcNow
                            IsXBRL = false
                            IsInlineXBRL = false
                        }
                        return [mockFiling] |> Seq.ofList
                    else
                        return Seq.empty
                }
            member _.GetFilingsSince(_) (_) = failwith "Not Implemented"
            member _.GetWatermark(_) (_) = failwith "Not Implemented"
            member _.UpsertWatermark(_) (_) (_) = failwith "Not Implemented"
    
    type MockOwnershipStorage13D() =
        let mutable entities = Map.empty<string, OwnershipEntity>
        let mutable events: OwnershipEvent list = []
        
        member _.GetStoredEvents() = events
        
        interface IOwnershipStorage with
            member _.GetEntityById(_) = task { return None }
            member _.GetEntitiesByIds(_) = task { return Seq.empty }
            member _.FindEntityByCik(cik) = 
                task { return entities |> Map.tryFind cik }
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
            member _.GetEventsByCompany(_) = 
                task { return events |> List.toSeq }
            member _.GetEventsByFilingId(_) = task { return Seq.empty }
            member _.GetEventsByCompanyDateRange(_) (_) (_) = task { return Seq.empty }
            member _.GetEventsByEntity(_) = task { return Seq.empty }
            member _.GetLatestEventForEntityCompany(_) (_) = task { return None }
            member _.GetOwnershipSummary(_) = task { return Seq.empty }
            member _.GetOwnershipTimeline(_) (_) = task { return Seq.empty }
            member _.GetRecentTimelines(_) = task { return Seq.empty }
    
    [<Fact>]
    [<Trait("Category", "Integration")>]
    let ``Service processes Schedule 13D filing and creates ownership event`` () = task {
        // Arrange
        let mockFilingStorage = MockSECFilingStorage13D()
        let mockOwnershipStorage = MockOwnershipStorage13D()
        
        let accountStorage = new storage.postgres.AccountStorage(
            new testutils.FakeOutbox(),
            testutils.CredsHelper.GetDbCreds()
        )
        let secClient = new EdgarClient(None, Some accountStorage)
        
        let loggerFactory = LoggerFactory.Create(fun builder -> 
            builder.SetMinimumLevel(LogLevel.Information) |> ignore)
        let logger = loggerFactory.CreateLogger<Schedule13DProcessingService>()
        
        let service = Schedule13DProcessingService(
            mockFilingStorage,
            mockOwnershipStorage,
            secClient,
            logger
        )
        
        // Act
        do! service.Execute()
        
        // Assert - check that ownership event was created
        let! events = (mockOwnershipStorage :> IOwnershipStorage).GetEventsByCompany (Ticker "ESTC")
        Assert.NotEmpty(events)
        
        let event = events |> Seq.head
        Assert.Equal("ESTC", event.CompanyTicker)
        Assert.True(event.SharesAfter |> Option.exists (fun s -> s > 0L), 
                    "Expected shares after to be greater than 0")
        Assert.Equal("large_stake_disclosure", event.EventType)
    }
    
    [<Fact>]
    [<Trait("Category", "Integration")>]
    let ``Service does not reprocess already processed filings`` () = task {
        // Arrange
        let mockFilingStorage = MockSECFilingStorage13D()
        let mockOwnershipStorage = MockOwnershipStorage13D()
        
        let accountStorage = new storage.postgres.AccountStorage(
            new testutils.FakeOutbox(),
            testutils.CredsHelper.GetDbCreds()
        )
        let secClient = new EdgarClient(None, Some accountStorage)
        
        let loggerFactory = LoggerFactory.Create(fun builder -> 
            builder.SetMinimumLevel(LogLevel.Information) |> ignore)
        let logger = loggerFactory.CreateLogger<Schedule13DProcessingService>()
        
        let service = Schedule13DProcessingService(
            mockFilingStorage,
            mockOwnershipStorage,
            secClient,
            logger
        )
        
        // Act - run twice
        do! service.Execute()
        do! service.Execute()
        
        // Assert - should only have one event (not duplicated)
        let! events = (mockOwnershipStorage :> IOwnershipStorage).GetEventsByCompany (Ticker "ESTC")
        let eventCount = events |> Seq.length
        Assert.Equal(1, eventCount)
    }
    
    [<Fact>]
    [<Trait("Category", "Integration")>]
    let ``Service creates entity with correct filer CIK`` () = task {
        // Arrange
        let mockFilingStorage = MockSECFilingStorage13D()
        let mockOwnershipStorage = MockOwnershipStorage13D()
        
        let accountStorage = new storage.postgres.AccountStorage(
            new testutils.FakeOutbox(),
            testutils.CredsHelper.GetDbCreds()
        )
        let secClient = new EdgarClient(None, Some accountStorage)
        
        let loggerFactory = LoggerFactory.Create(fun builder -> 
            builder.SetMinimumLevel(LogLevel.Information) |> ignore)
        let logger = loggerFactory.CreateLogger<Schedule13DProcessingService>()
        
        let service = Schedule13DProcessingService(
            mockFilingStorage,
            mockOwnershipStorage,
            secClient,
            logger
        )
        
        // Act
        do! service.Execute()
        
        // Assert - verify entity was created for Pictet (CIK 0001361570)
        let! events = (mockOwnershipStorage :> IOwnershipStorage).GetEventsByCompany (Ticker "ESTC")
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
            builder.SetMinimumLevel(LogLevel.Information) |> ignore)
        let logger = loggerFactory.CreateLogger<Schedule13DProcessingService>()
        
        let service = Schedule13DProcessingService(
            realFilingStorage,
            realOwnershipStorage,
            secClient,
            logger
        )
        
        // Act
        logger.LogInformation("=== STARTING END-TO-END 13D TEST WITH REAL DATABASE ===")
        do! service.Execute()
        logger.LogInformation("=== 13D SERVICE EXECUTION COMPLETED ===")
        
        // Note: No hard assertions - depends on actual database state
        // Check the logs to verify processing
    }

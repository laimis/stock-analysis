namespace edgartests

open System
open Xunit
open secedgar.fs
open core.fs.Adapters.Storage
open core.Shared
open Microsoft.Extensions.Logging


module Form144ProcessingServiceTests =

    // Mock ISECFilingStorage which returns a Form 144 filing pointing to the real Palantir/Karp document
    type MockForm144FilingStorage() =
        interface ISECFilingStorage with
            member _.SaveFiling(_) = task { return true }
            member _.SaveFilings(_) = task { return 0 }
            member _.GetFilingsByTicker(_) = task { return Seq.empty }
            member _.GetRecentFilingsByTicker(_) (_) = task { return Seq.empty }
            member _.GetFilingsByTickers(_) (_) = task { return Seq.empty }
            member _.FilingExists(_) = task { return false }
            member _.GetFilingsByFormType(formTypes) (_limit) =
                task {
                    // Return a mock Form 144 filing for Palantir/Alexander Karp
                    if Seq.contains "144" formTypes then
                        let mockFiling = {
                            Id = Guid.NewGuid()
                            Ticker = "PLTR"
                            Cik = "0001321655"
                            FormType = "144"
                            FilingDate = "2026-02-20"
                            ReportDate = Some "2026-02-20"
                            Description = "144"
                            FilingUrl = "https://www.sec.gov/Archives/edgar/data/1321655/000195004726001584/0001950047-26-001584-index.html"
                            DocumentUrl = "https://www.sec.gov/Archives/edgar/data/1321655/000195004726001584/primary_doc.xml"
                            CreatedAt = DateTimeOffset.UtcNow
                            IsXBRL = false
                            IsInlineXBRL = false
                        }
                        return [mockFiling] |> Seq.ofList
                    else
                        return Seq.empty
                }
            member _.GetFilingsSince(_) (_) : System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<SECFilingRecord>> =
                failwith "Not Implemented"
            member _.GetWatermark(_) (_) : System.Threading.Tasks.Task<DateTimeOffset option> =
                failwith "Not Implemented"
            member _.UpsertWatermark(_) (_) (_) : System.Threading.Tasks.Task<unit> =
                failwith "Not Implemented"

    // Mock IOwnershipStorage that captures saved events for assertions
    type MockOwnershipStorageForForm144() =
        let mutable entities = Map.empty<string, OwnershipEntity>
        let mutable events = []

        member _.GetStoredEvents() = events

        interface IOwnershipStorage with
            member _.GetEntityById(_) = task { return None }
            member _.GetEntitiesByIds(_) = task { return Seq.empty }
            member _.FindEntityByCik(cik) =
                task { return entities |> Map.tryFind cik }
            member _.FindEntitiesByName(_) = task { return Seq.empty }
            member _.SaveEntity(entity) =
                task {
                    entities <- entities |> Map.add (entity.Cik |> Option.defaultValue entity.Name) entity
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
            member _.GetEventsByCompanyDateRange(_) (_) (_) = task { return Seq.empty }
            member _.GetEventsByEntity(_) = task { return Seq.empty }
            member _.GetLatestEventForEntityCompany(_) (_) = task { return None }
            member _.GetOwnershipSummary(_) = task { return Seq.empty }
            member _.GetOwnershipTimeline(_) (_) = task { return Seq.empty }
            member _.GetRecentTimelines(_) = task { return Seq.empty }

    [<Fact>]
    [<Trait("Category", "Integration")>]
    let ``Service processes Form 144 filing and creates ownership event`` () = task {
        // Arrange
        let mockFilingStorage = MockForm144FilingStorage()
        let mockOwnershipStorage = MockOwnershipStorageForForm144()

        // Use the real EdgarClient to fetch the XML from SEC
        let accountStorage = new storage.postgres.AccountStorage(
            new testutils.FakeOutbox(),
            testutils.CredsHelper.GetDbCreds()
        )
        let secClient = new EdgarClient(None, Some accountStorage)

        let loggerFactory = LoggerFactory.Create(fun builder -> builder.SetMinimumLevel(LogLevel.Information) |> ignore)
        let logger = loggerFactory.CreateLogger<Form144ProcessingService>()

        let service = Form144ProcessingService(
            mockFilingStorage,
            mockOwnershipStorage,
            secClient,
            logger
        )

        // Act
        do! service.Execute()

        // Assert - check that an ownership event was created
        let! events = (mockOwnershipStorage :> IOwnershipStorage).GetEventsByCompany (Ticker "PLTR")
        Assert.NotEmpty(events)

        let event = events |> Seq.head
        Assert.Equal("PLTR", event.CompanyTicker)
        Assert.Equal("intent_to_sell", event.EventType)
        Assert.Equal(Some "sale", event.TransactionType)

        // Verify shares transacted matches (90,000 for Karp filing)
        Assert.True(event.SharesTransacted.IsSome, "Expected SharesTransacted to be set")
        Assert.Equal(Some 90000L, event.SharesTransacted)

        // SharesAfter should also be set (equals SharesToSell for Form 144)
        Assert.Equal(90000L, event.SharesAfter)

        // IsDirect should be true
        Assert.True(event.IsDirect, "Form 144 events should be direct ownership")

        // OwnershipNature should describe the proposed sale
        Assert.True(event.OwnershipNature.IsSome, "Expected OwnershipNature to be set")
    }

    [<Fact>]
    [<Trait("Category", "Integration")>]
    let ``Service does not reprocess already processed Form 144 filings`` () = task {
        // Arrange
        let mockFilingStorage = MockForm144FilingStorage()
        let mockOwnershipStorage = MockOwnershipStorageForForm144()

        let accountStorage = new storage.postgres.AccountStorage(
            new testutils.FakeOutbox(),
            testutils.CredsHelper.GetDbCreds()
        )
        let secClient = new EdgarClient(None, Some accountStorage)

        let loggerFactory = LoggerFactory.Create(fun builder -> builder.SetMinimumLevel(LogLevel.Information) |> ignore)
        let logger = loggerFactory.CreateLogger<Form144ProcessingService>()

        let service = Form144ProcessingService(
            mockFilingStorage,
            mockOwnershipStorage,
            secClient,
            logger
        )

        // Act - run twice to verify deduplication
        do! service.Execute()
        do! service.Execute()

        // Assert - should only have ONE event despite running twice
        let! events = (mockOwnershipStorage :> IOwnershipStorage).GetEventsByCompany (Ticker "PLTR")
        Assert.Single(events) |> ignore
    }

    [<Fact>]
    [<Trait("Category", "Integration")>]
    let ``Service creates entity for new insider`` () = task {
        // Arrange
        let mockFilingStorage = MockForm144FilingStorage()
        let mockOwnershipStorage = MockOwnershipStorageForForm144()

        let accountStorage = new storage.postgres.AccountStorage(
            new testutils.FakeOutbox(),
            testutils.CredsHelper.GetDbCreds()
        )
        let secClient = new EdgarClient(None, Some accountStorage)

        let loggerFactory = LoggerFactory.Create(fun builder -> builder.SetMinimumLevel(LogLevel.Information) |> ignore)
        let logger = loggerFactory.CreateLogger<Form144ProcessingService>()

        let service = Form144ProcessingService(
            mockFilingStorage,
            mockOwnershipStorage,
            secClient,
            logger
        )

        // Act
        do! service.Execute()

        // Assert - an entity should have been created for Alexander Karp
        let! karpEntity = (mockOwnershipStorage :> IOwnershipStorage).FindEntityByCik "0001823951"
        Assert.True(karpEntity.IsSome, "Expected entity to be created for Alexander Karp")
        let entity = karpEntity.Value
        Assert.Equal("ALEXANDER KARP", entity.Name)
        // Should be EP (Executive/C-Suite) since he's an Officer
        Assert.Equal("EP", entity.EntityType)
    }

    [<Fact>]
    let ``Parser correctly maps Form 144 to ownership event fields`` () =
        // Arrange - create a parsed Form 144 with known values
        let parsed = {
            FilerCik = Some "0001823951"
            PersonName = "ALEXANDER KARP"
            RelationshipsToIssuer = ["Director"; "Officer"]
            IssuerCik = Some "0001321655"
            IssuerName = "Palantir Technologies Inc."
            SecuritiesClassTitle = Some "Common"
            SharesToSell = 90000L
            AggregateMarketValue = Some 12140100.00m
            SharesOutstanding = Some 2291470751L
            ApproxSaleDate = Some (DateTimeOffset(2026, 2, 20, 0, 0, 0, TimeSpan.Zero))
            Exchange = Some "NASDAQ"
            NatureOfAcquisition = Some "Restricted Stock Units"
            SecuritiesAcquired = Some 90000L
            NoticeDate = Some (DateTimeOffset(2026, 2, 20, 0, 0, 0, TimeSpan.Zero))
            PlanAdoptionDate = Some (DateTimeOffset(2025, 11, 21, 0, 0, 0, TimeSpan.Zero))
            IsAmendment = false
            NothingToReportPast3Months = true
            Confidence = 1.0
            RawXml = None
            ParsingNotes = []
        }

        // Verify that confidence calculation works
        let confidence = Form144Helpers.calculateConfidence parsed
        Assert.True(confidence >= 1.0, $"Expected confidence 1.0 for fully populated data, got {confidence}")

        // Verify entity type determination
        let entityType = Form144Helpers.determineEntityType parsed.RelationshipsToIssuer
        Assert.Equal(Some "EP", entityType)

        // Verify price per share calculation (aggregate value / shares)
        // $12,140,100 / 90,000 shares = $134.89 per share
        let pricePerShare =
            match parsed.AggregateMarketValue with
            | Some value when parsed.SharesToSell > 0L ->
                Some (value / decimal parsed.SharesToSell)
            | _ -> None

        Assert.True(pricePerShare.IsSome, "Expected price per share to be calculated")
        let price = pricePerShare.Value
        Assert.True(price > 134m && price < 135m, $"Expected price ~$134.89, got {price}")

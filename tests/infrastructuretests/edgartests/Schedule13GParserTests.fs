namespace edgartests

open System
open System.IO
open Xunit
open secedgar.fs


module EdgarClientTests =
    open core.fs.Adapters.SEC
    open core.Shared

    let createEdgarClient() =
        let accountStorage = new storage.postgres.AccountStorage(
            new testutils.FakeOutbox(),
           testutils.CredsHelper.GetDbCreds()
        )
        new EdgarClient(None, Some accountStorage) :> ISECFilings

    [<Theory>]
    [<InlineData("AAPL")>]
    [<InlineData("MSFT")>]
    [<InlineData("DOCN")>]
    [<Trait("Category", "Integration")>]
    let ``Fetch filings for ticker works`` (ticker: string) = task {

        let client = createEdgarClient()
        let! result = ticker |> Ticker |> client.GetFilings

        match result with
        | Ok companyFilings ->
            Assert.Equal(ticker, companyFilings.Ticker.Value)
            Assert.NotEmpty(companyFilings.Filings)

        | Error err ->
            Assert.Fail($"Failed to fetch filings for {ticker}: {err}")
    }
    
    [<Fact>]
    [<Trait("Category", "Integration")>]
    let ``Fetch and parse Schedule 13G from SEC`` () = task {
        // Arrange
        let client = createEdgarClient()

        let! filings = "DOCS" |> Ticker |> client.GetFilings

        // Assert - we expect to get at least one filing for DOCS
        match filings with
        | Ok companyFilings ->
            Assert.NotEmpty(companyFilings.Filings)
            
            // Find a 13G filing (not all filings are 13G)
            let filing13G = companyFilings.Filings 
                            |> Seq.tryFind (fun f -> f.Filing.Contains("13G", StringComparison.OrdinalIgnoreCase))
            
            Assert.True(filing13G.IsSome, "Expected to find at least one 13G filing for DOCS")

            // now, we have to parse the filing URL to get the actual XML URL for the Schedule 13G document
            let filingUrl = filing13G.Value.FilingUrl
            let! primaryDocument = client.FetchPrimaryDocument filingUrl
            
            match primaryDocument with
            | Ok content ->
                Assert.False(String.IsNullOrEmpty content, "XML content should not be empty")
                
                // now let's parse it using our Schedule13GParser
                let result = Schedule13GParser.parseFromDocument None content
                match result with
                | Success parsed ->
                    Assert.True(String.IsNullOrEmpty parsed.FilerName |> not, "Filer name should not be empty")
                    Assert.Contains("DOXIMITY", parsed.IssuerName.ToUpper())
                    Assert.True(parsed.SharesOwned > 0, "Expected shares owned to be greater than 0")
                    Assert.True(parsed.PercentOfClass > 0m, "Expected percent of class to be greater than 0")
                    Assert.True(parsed.Confidence > 0.5, $"Expected confidence > 0.5, got {parsed.Confidence}")
                | PartialSuccess (parsed, notes) ->
                    let notesStr = String.Join("; ", notes)
                    Assert.True(parsed.Confidence > 0.4, 
                                $"Confidence too low even for partial success: {parsed.Confidence}. Notes: {notesStr}")
                | Failure msg ->
                    Assert.Fail($"Failed to parse Schedule 13G XML: {msg}")
            | Error err ->
                Assert.Fail($"Failed to get XML URL: {err}")
        | Error err ->
            Assert.Fail($"Failed to fetch filings: {err}")
    }

module Schedule13GParserTests =
    
    [<Fact>]
    let ``Parse sample Schedule 13G XML - DOCS/FMR`` () =
        // Arrange
        let samplePath = Path.Combine(AppContext.BaseDirectory, "sample_schedule_13g.xml")
        Assert.True(File.Exists(samplePath), $"Sample file not found: {samplePath}")
        
        // Act
        let result = Schedule13GParser.parseFromFile None samplePath
        
        // Assert
        match result with
        | Success parsed ->
            Assert.Equal("FMR LLC", parsed.FilerName)
            Assert.Equal(Some "0000315066", parsed.FilerCik)
            Assert.Equal("DOXIMITY INC", parsed.IssuerName) // SEC uses all-caps, no punctuation
            Assert.Equal(Some "0001516513", parsed.IssuerCik)
            Assert.Equal(None, parsed.IssuerTicker) // Note: Ticker not in real SEC XML!
            Assert.Equal(2164072L, parsed.SharesOwned) // Rounded from 2164071.69
            Assert.Equal(1.6m, parsed.PercentOfClass)
            Assert.Equal(Some "HC", parsed.EntityType)
            Assert.Equal(Some 2155852L, parsed.SoleVotingPower) // Rounded from 2155851.97
            Assert.Equal(Some 0L, parsed.SharedVotingPower)
            Assert.Equal(Some 2164072L, parsed.SoleDispositivePower) // Rounded from 2164071.69
            Assert.Equal(Some 0L, parsed.SharedDispositivePower)
            Assert.True(parsed.IsAmendment)
            Assert.True(parsed.Confidence > 0.7, $"Expected confidence > 0.7, got {parsed.Confidence}")
        | PartialSuccess (parsed, notes) ->
            let notesStr = String.Join("; ", notes)
            Assert.Fail($"PartialSuccess: {notesStr}")
        | Failure msg ->
            Assert.Fail($"Failed to parse: {msg}")
    
    [<Fact>]
    [<Trait("Category", "Integration")>]
    let ``Parse real Schedule 13G from SEC - DOCS/FMR`` () = task {
        // Arrange
        let filingUrl = "https://www.sec.gov/Archives/edgar/data/1516513/000031506626000439/0000315066-26-000439-index.html"
        
        let client = EdgarClientTests.createEdgarClient()

        let! content = client.FetchPrimaryDocument filingUrl

        match content with
        | Error err ->
            Assert.Fail($"Failed to fetch primary document: {err}")
        | Ok xmlContent ->
        
            // Act
            let result = Schedule13GParser.parseFromDocument None xmlContent
            
            // Assert
            match result with
            | Success parsed ->
                // Verify core fields match expected values
                Assert.Contains("FMR", parsed.FilerName.ToUpper())
                Assert.True(parsed.FilerCik.IsSome, "Expected filer CIK")
                Assert.Contains("DOXIMITY", parsed.IssuerName.ToUpper())
                Assert.Equal(Some "0001516513", parsed.IssuerCik)
                
                // Ownership should be roughly 2.16M shares
                let lowerBound = 2164000L // Allow for rounding from 2164071.69
                let upperBound = 2165000L
                Assert.True(parsed.SharesOwned > lowerBound && parsed.SharesOwned < upperBound, 
                            $"Expected ~2.16M shares, got {parsed.SharesOwned}")
                
                // Percent should be around 1.6%
                Assert.True(parsed.PercentOfClass > 1.5m && parsed.PercentOfClass < 1.7m,
                            $"Expected approximately 1.6 percent, got {parsed.PercentOfClass}")
                
                // Should have reasonable confidence
                Assert.True(parsed.Confidence > 0.5, $"Expected confidence > 0.5, got {parsed.Confidence}")
                
            | PartialSuccess (parsed, notes) ->
                // Partial success is acceptable - log warning but don't fail
                // Real filings may have variations we don't handle perfectly
                let notesStr = String.Join("; ", notes)
                Assert.True(parsed.Confidence > 0.4, 
                            $"Confidence too low even for partial success: {parsed.Confidence}. Notes: {notesStr}")
                
            | Failure msg ->
                // For now, just fail - network issues will be evident from the error message
                Assert.Fail($"Failed to parse: {msg}")
    }
    
    [<Fact>]
    let ``Confidence calculation works correctly`` () =
        // Arrange - minimal parsed data
        let minimal = { Schedule13GHelpers.empty with
                          FilerName = "Test Filer"
                          IssuerName = "Test Company" }
        
        // Act
        let confidence = Schedule13GHelpers.calculateConfidence minimal
        
        // Assert - should have low confidence with minimal data
        Assert.True(confidence < 0.5, $"Expected low confidence, got {confidence}")
        
        // Arrange - well-populated data
        let complete = { Schedule13GHelpers.empty with
                           FilerName = "Test Filer"
                           FilerCik = Some "0000123456"
                           IssuerName = "Test Company"
                           IssuerCik = Some "0001234567"
                           SharesOwned = 1_000_000L
                           PercentOfClass = 5.0m
                           EntityType = Some "IA"
                           SoleVotingPower = Some 1_000_000L
                           AsOfDate = Some DateTimeOffset.UtcNow }
        
        // Act
        let highConfidence = Schedule13GHelpers.calculateConfidence complete
        
        // Assert - should have high confidence with complete data
        Assert.True(highConfidence > 0.8, $"Expected high confidence, got {highConfidence}")
    
    [<Fact>]
    let ``Entity type mapping works correctly`` () =
        Assert.Equal(Some "IA", Schedule13GHelpers.mapEntityType (Some "IA"))
        Assert.Equal(Some "HC", Schedule13GHelpers.mapEntityType (Some "HC"))
        Assert.Equal(Some "FI", Schedule13GHelpers.mapEntityType (Some "BK"))
        Assert.Equal(None, Schedule13GHelpers.mapEntityType (Some "UNKNOWN"))
        Assert.Equal(None, Schedule13GHelpers.mapEntityType None)
    
    [<Fact>]
    let ``Parser handles missing elements gracefully`` () =
        // Arrange - minimal XML with just root element
        let minimalXml = """<?xml version="1.0" encoding="UTF-8"?>
<edgarSubmission xmlns="http://www.sec.gov/edgar/schedule13g">
    <submissionType>SC 13G</submissionType>
</edgarSubmission>"""
        
        // Act
        let result = Schedule13GParser.parseXml minimalXml None
        
        // Assert - should fail or partial success due to missing data
        match result with
        | Success _ -> Assert.Fail("Should not succeed with minimal XML")
        | PartialSuccess (parsed, notes) -> 
            Assert.True(notes.Length > 0, "Should have parsing notes")
            Assert.True(parsed.Confidence < 0.5, "Should have low confidence")
        | Failure _ -> () // Expected failure is acceptable

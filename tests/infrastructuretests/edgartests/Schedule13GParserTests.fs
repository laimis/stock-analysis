namespace edgartests

open System
open System.IO
open System.Net.Http
open Xunit
open secedgar.fs

module Schedule13GParserTests =
    
    [<Fact>]
    let ``Parse sample Schedule 13G XML - DOCS/FMR`` () =
        // Arrange
        let samplePath = Path.Combine(AppContext.BaseDirectory, "sample_schedule_13g.xml")
        Assert.True(File.Exists(samplePath), $"Sample file not found: {samplePath}")
        
        // Act
        let result = Schedule13GParser.parseFromFile samplePath None
        
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
    let ``Parse real Schedule 13G from SEC - DOCS/FMR`` () =
        // Arrange
        let filingUrl = "https://www.sec.gov/Archives/edgar/data/1516513/000031506626000439/xslSCHEDULE_13G_X01/primary_doc.xml"
        
        // Create HttpClient with proper SEC headers (matching EdgarClient pattern)
        use httpClient = new HttpClient()
        httpClient.DefaultRequestHeaders.Clear()
        httpClient.DefaultRequestHeaders.Add("User-Agent", "NGTDTrading/1.0 (secclient@nightingaletrading.com)")
        
        // Act
        let result = Schedule13GParser.parseFromUrl filingUrl httpClient None |> Async.RunSynchronously
        
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

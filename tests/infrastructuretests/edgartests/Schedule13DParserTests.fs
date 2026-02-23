namespace edgartests

open System
open System.IO
open Xunit
open secedgar.fs

module Schedule13DParserTests =
    
    [<Fact>]
    let ``Parse sample Schedule 13D XML - Elastic NV / Pictet Asset Management`` () =
        // Arrange
        let samplePath = Path.Combine(AppContext.BaseDirectory, "sample_schedule_13d.xml")
        Assert.True(File.Exists(samplePath), $"Sample file not found: {samplePath}")
        
        // Act
        let result = Schedule13DParser.parseFromFile None samplePath
        
        // Assert
        match result with
        | Schedule13DSuccess parsed ->
            Assert.Equal("PICTET ASSET MANAGEMENT SA", parsed.FilerName)
            Assert.Equal(Some "0001361570", parsed.FilerCik)
            Assert.Equal("Elastic N.V.", parsed.IssuerName)
            Assert.Equal(Some "0001707753", parsed.IssuerCik)
            Assert.Equal(Some "N14506104", parsed.IssuerCusip)
            Assert.Equal(5_288_262L, parsed.SharesOwned)
            Assert.Equal(5m, parsed.PercentOfClass)
            Assert.Equal(Some 5_274_370L, parsed.SoleVotingPower)
            Assert.Equal(Some "COMMON STOCK", parsed.SecuritiesClassTitle)
            Assert.Equal(Some "SWITZERLAND", parsed.Citizenship)
            Assert.False(parsed.IsAmendment)
            Assert.True(parsed.AsOfDate.IsSome, "Expected AsOfDate to be present")
            // Check date components (not offset, which is machine-dependent)
            Assert.Equal(2026, parsed.AsOfDate.Value.Year)
            Assert.Equal(1, parsed.AsOfDate.Value.Month)
            Assert.Equal(23, parsed.AsOfDate.Value.Day)
            Assert.True(parsed.Confidence > 0.7, $"Expected confidence > 0.7, got {parsed.Confidence}")
        | Schedule13DPartialSuccess (parsed, notes) ->
            let notesStr = String.Join("; ", notes)
            Assert.Fail($"PartialSuccess when full success expected: {notesStr}")
        | Schedule13DFailure msg ->
            Assert.Fail($"Failed to parse: {msg}")
    
    [<Fact>]
    let ``Filing date parsed correctly from MM/dd/yyyy format`` () =
        // Arrange
        let samplePath = Path.Combine(AppContext.BaseDirectory, "sample_schedule_13d.xml")
        Assert.True(File.Exists(samplePath), $"Sample file not found: {samplePath}")
        
        // Act
        let result = Schedule13DParser.parseFromFile None samplePath
        
        // Assert
        match result with
        | Schedule13DSuccess parsed ->
            // Signature date is 01/26/2026
            Assert.Equal(2026, parsed.FilingDate.Year)
            Assert.Equal(1, parsed.FilingDate.Month)
            Assert.Equal(26, parsed.FilingDate.Day)
        | Schedule13DPartialSuccess (parsed, _) ->
            Assert.Equal(2026, parsed.FilingDate.Year)
        | Schedule13DFailure msg ->
            Assert.Fail($"Failed to parse: {msg}")
    
    [<Fact>]
    let ``Amendment detection works for 13D/A`` () =
        // Arrange - construct a minimal amendment XML
        let amendmentXml = """<?xml version="1.0" encoding="UTF-8"?>
<edgarSubmission xmlns="http://www.sec.gov/edgar/schedule13D">
  <headerData>
    <submissionType>SCHEDULE 13D/A</submissionType>
    <filerInfo>
      <filer>
        <filerCredentials>
          <cik>0001361570</cik>
          <ccc>XXXXXXXX</ccc>
        </filerCredentials>
      </filer>
    </filerInfo>
  </headerData>
  <formData>
    <coverPageHeader>
      <dateOfEvent>02/01/2026</dateOfEvent>
      <issuerInfo>
        <issuerCIK>0001707753</issuerCIK>
        <issuerName>Elastic N.V.</issuerName>
      </issuerInfo>
    </coverPageHeader>
    <reportingPersons>
      <reportingPersonInfo>
        <reportingPersonName>PICTET ASSET MANAGEMENT SA</reportingPersonName>
        <reportingPersonCIK>0001361570</reportingPersonCIK>
        <typeOfReportingPerson>IA</typeOfReportingPerson>
        <aggregateAmountOwned>6000000</aggregateAmountOwned>
        <percentOfClass>5.70</percentOfClass>
        <soleVotingPower>6000000</soleVotingPower>
        <sharedVotingPower>0</sharedVotingPower>
        <soleDispositivePower>6000000</soleDispositivePower>
        <sharedDispositivePower>0</sharedDispositivePower>
      </reportingPersonInfo>
    </reportingPersons>
    <signatureInfo>
      <signaturePerson>
        <signatureReportingPerson>PICTET ASSET MANAGEMENT SA</signatureReportingPerson>
        <signatureDetails>
          <date>02/05/2026</date>
        </signatureDetails>
      </signaturePerson>
    </signatureInfo>
  </formData>
</edgarSubmission>"""
        
        // Act
        let result = Schedule13DParser.parseFromDocument None amendmentXml
        
        // Assert
        match result with
        | Schedule13DSuccess parsed | Schedule13DPartialSuccess (parsed, _) ->
            Assert.True(parsed.IsAmendment, "Expected IsAmendment to be true for 13D/A")
            Assert.Equal(6_000_000L, parsed.SharesOwned)
            Assert.Equal(5.70m, parsed.PercentOfClass)
        | Schedule13DFailure msg ->
            Assert.Fail($"Failed to parse amendment: {msg}")
    
    [<Fact>]
    let ``Shares parsed from structured XML with apostrophe thousands separator`` () =
        // Arrange - Swiss-style thousands separator in aggregateAmountOwned (e.g. Pictet format)
        let xml = """<?xml version="1.0" encoding="UTF-8"?>
<edgarSubmission xmlns="http://www.sec.gov/edgar/schedule13D">
  <headerData>
    <submissionType>SCHEDULE 13D</submissionType>
    <filerInfo><filer><filerCredentials><cik>0001234567</cik><ccc>X</ccc></filerCredentials></filer></filerInfo>
  </headerData>
  <formData>
    <coverPageHeader>
      <dateOfEvent>03/01/2026</dateOfEvent>
      <issuerInfo>
        <issuerCIK>0009876543</issuerCIK>
        <issuerName>Test Corp</issuerName>
      </issuerInfo>
    </coverPageHeader>
    <reportingPersons>
      <reportingPersonInfo>
        <reportingPersonName>TEST FILER</reportingPersonName>
        <reportingPersonCIK>0001234567</reportingPersonCIK>
        <typeOfReportingPerson>IA</typeOfReportingPerson>
        <aggregateAmountOwned>1'234'567</aggregateAmountOwned>
        <percentOfClass>6.78</percentOfClass>
        <soleVotingPower>1'234'567</soleVotingPower>
        <sharedVotingPower>0</sharedVotingPower>
        <soleDispositivePower>1'234'567</soleDispositivePower>
        <sharedDispositivePower>0</sharedDispositivePower>
      </reportingPersonInfo>
    </reportingPersons>
    <signatureInfo>
      <signaturePerson>
        <signatureReportingPerson>TEST FILER</signatureReportingPerson>
        <signatureDetails><date>03/05/2026</date></signatureDetails>
      </signaturePerson>
    </signatureInfo>
  </formData>
</edgarSubmission>"""
        
        // Act
        let result = Schedule13DParser.parseFromDocument None xml
        
        // Assert
        match result with
        | Schedule13DSuccess parsed | Schedule13DPartialSuccess (parsed, _) ->
            Assert.Equal(1_234_567L, parsed.SharesOwned)
            Assert.Equal(6.78m, parsed.PercentOfClass)
        | Schedule13DFailure msg ->
            Assert.Fail($"Failed to parse: {msg}")
    
    [<Fact>]
    let ``Confidence calculation works correctly for 13D`` () =
        // Arrange - minimal parsed data
        let minimal = { Schedule13DHelpers.empty with
                          FilerName = "Test Filer"
                          IssuerName = "Test Company" }
        
        // Act
        let confidence = Schedule13DHelpers.calculateConfidence minimal
        
        // Assert - should have low confidence with minimal data
        Assert.True(confidence < 0.5, $"Expected low confidence, got {confidence}")
        
        // Arrange - well-populated data
        let complete = { Schedule13DHelpers.empty with
                           FilerName = "Test Filer"
                           FilerCik = Some "0000123456"
                           IssuerName = "Test Company"
                           IssuerCik = Some "0001234567"
                           SharesOwned = 5_000_000L
                           PercentOfClass = 5.0m
                           EntityType = Some "OO"
                           SoleVotingPower = Some 5_000_000L
                           AsOfDate = Some DateTimeOffset.UtcNow }
        
        // Act
        let highConfidence = Schedule13DHelpers.calculateConfidence complete
        
        // Assert - should have high confidence with complete data
        Assert.True(highConfidence > 0.8, $"Expected high confidence, got {highConfidence}")
    
    [<Fact>]
    let ``Parser handles missing elements gracefully`` () =
        // Arrange - minimal XML with just root element
        let minimalXml = """<?xml version="1.0" encoding="UTF-8"?>
<edgarSubmission xmlns="http://www.sec.gov/edgar/schedule13D">
    <submissionType>SCHEDULE 13D</submissionType>
</edgarSubmission>"""
        
        // Act
        let result = Schedule13DParser.parseXml minimalXml None
        
        // Assert - should fail or partial success due to missing data
        match result with
        | Schedule13DSuccess _ -> 
            Assert.Fail("Should not succeed with minimal XML")
        | Schedule13DPartialSuccess (parsed, notes) -> 
            Assert.True(notes.Length > 0, "Should have parsing notes")
            Assert.True(parsed.Confidence < 0.5, "Should have low confidence")
        | Schedule13DFailure _ -> 
            () // Expected failure is acceptable
    
    [<Fact>]
    [<Trait("Category", "Integration")>]
    let ``Parse real Schedule 13D from SEC - Elastic NV / Pictet`` () = task {
        // Arrange
        let client = EdgarClientTests.createEdgarClient()
        
        let filingUrl = "https://www.sec.gov/Archives/edgar/data/1707753/000136157026000003/0001361570-26-000003-index.html"
        
        let! content = client.FetchPrimaryDocument filingUrl
        
        match content with
        | Error err ->
            Assert.Fail($"Failed to fetch primary document: {err}")
        | Ok xmlContent ->
            // Act
            let result = Schedule13DParser.parseFromDocument None xmlContent
            
            // Assert
            match result with
            | Schedule13DSuccess parsed | Schedule13DPartialSuccess (parsed, _) ->
                Assert.Contains("PICTET", parsed.FilerName.ToUpper())
                Assert.True(parsed.FilerCik.IsSome, "Expected filer CIK")
                Assert.Contains("ELASTIC", parsed.IssuerName.ToUpper())
                Assert.Equal(Some "0001707753", parsed.IssuerCik)
                Assert.True(parsed.SharesOwned > 5_000_000L, $"Expected >5M shares, got {parsed.SharesOwned}")
                Assert.True(parsed.PercentOfClass > 4.0m, $"Expected >4 percent, got {parsed.PercentOfClass}")
                Assert.True(parsed.Confidence > 0.5, $"Expected confidence > 0.5, got {parsed.Confidence}")
            | Schedule13DFailure msg ->
                Assert.Fail($"Failed to parse real SEC filing: {msg}")
    }

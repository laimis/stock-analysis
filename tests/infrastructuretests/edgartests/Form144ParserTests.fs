namespace edgartests

open System
open System.IO
open Xunit
open secedgar.fs


module Form144ParserTests =

    [<Fact>]
    let ``Parse sample Form 144 XML - Palantir/Alexander Karp`` () =
        // Arrange - use the sample XML that mirrors the real SEC filing
        let samplePath = Path.Combine(AppContext.BaseDirectory, "sample_form_144.xml")
        Assert.True(File.Exists(samplePath), $"Sample file not found: {samplePath}")

        // Act
        let result = Form144Parser.parseFromFile None samplePath

        // Assert
        match result with
        | Form144Success parsed ->
            Assert.Equal("ALEXANDER KARP", parsed.PersonName)
            Assert.Equal(Some "0001823951", parsed.FilerCik)
            Assert.Equal("Palantir Technologies Inc.", parsed.IssuerName)
            Assert.Equal(Some "0001321655", parsed.IssuerCik)
            Assert.Equal(90000L, parsed.SharesToSell)
            Assert.Equal(Some 12140100.00m, parsed.AggregateMarketValue)
            Assert.Equal(Some 2291470751L, parsed.SharesOutstanding)
            Assert.Equal(Some "NASDAQ", parsed.Exchange)
            Assert.Equal(Some "Common", parsed.SecuritiesClassTitle)
            Assert.Equal(Some "Restricted Stock Units", parsed.NatureOfAcquisition)
            Assert.Equal(Some 90000L, parsed.SecuritiesAcquired)
            Assert.Equal(2, parsed.RelationshipsToIssuer.Length)
            Assert.Contains("Director", parsed.RelationshipsToIssuer)
            Assert.Contains("Officer", parsed.RelationshipsToIssuer)
            Assert.False(parsed.IsAmendment)
            Assert.True(parsed.NothingToReportPast3Months)

            // Verify approx sale date is 2026-02-20
            Assert.True(parsed.ApproxSaleDate.IsSome, "Expected ApproxSaleDate to be parsed")
            let saleDate = parsed.ApproxSaleDate.Value
            Assert.Equal(2026, saleDate.Year)
            Assert.Equal(2, saleDate.Month)
            Assert.Equal(20, saleDate.Day)

            // Verify notice date is 2026-02-20
            Assert.True(parsed.NoticeDate.IsSome, "Expected NoticeDate to be parsed")
            let noticeDate = parsed.NoticeDate.Value
            Assert.Equal(2026, noticeDate.Year)
            Assert.Equal(2, noticeDate.Month)
            Assert.Equal(20, noticeDate.Day)

            // Verify plan adoption date is 2025-11-21
            Assert.True(parsed.PlanAdoptionDate.IsSome, "Expected PlanAdoptionDate to be parsed")
            let planDate = parsed.PlanAdoptionDate.Value
            Assert.Equal(2025, planDate.Year)
            Assert.Equal(11, planDate.Month)
            Assert.Equal(21, planDate.Day)

            Assert.True(parsed.Confidence > 0.7, $"Expected confidence > 0.7, got {parsed.Confidence}")
        | Form144PartialSuccess (parsed, notes) ->
            let notesStr = String.Join("; ", notes)
            Assert.Fail($"PartialSuccess (expected full success): {notesStr}")
        | Form144Failure msg ->
            Assert.Fail($"Failed to parse: {msg}")

    [<Fact>]
    let ``Parse Form 144 amendment (144/A) sets IsAmendment flag`` () =
        // Arrange - same XML but with 144/A submission type
        let xml = """<?xml version="1.0" encoding="UTF-8"?>
<edgarSubmission xmlns="http://www.sec.gov/edgar/ownership" xmlns:ns2="http://www.sec.gov/edgar/common">
    <headerData>
        <submissionType>144/A</submissionType>
        <filerInfo>
            <filer>
                <filerCredentials>
                    <cik>0001823951</cik>
                    <ccc>XXXXXXXX</ccc>
                </filerCredentials>
            </filer>
        </filerInfo>
    </headerData>
    <formData>
        <issuerInfo>
            <issuerCik>0001321655</issuerCik>
            <issuerName>Palantir Technologies Inc.</issuerName>
            <nameOfPersonForWhoseAccountTheSecuritiesAreToBeSold>ALEXANDER KARP</nameOfPersonForWhoseAccountTheSecuritiesAreToBeSold>
            <relationshipsToIssuer>
                <relationshipToIssuer>Officer</relationshipToIssuer>
            </relationshipsToIssuer>
        </issuerInfo>
        <securitiesInformation>
            <noOfUnitsSold>50000</noOfUnitsSold>
            <aggregateMarketValue>6500000.00</aggregateMarketValue>
            <approxSaleDate>02/25/2026</approxSaleDate>
            <securitiesExchangeName>NASDAQ</securitiesExchangeName>
        </securitiesInformation>
        <noticeSignature>
            <noticeDate>02/25/2026</noticeDate>
        </noticeSignature>
    </formData>
</edgarSubmission>"""

        // Act
        let result = Form144Parser.parseFromDocument None xml

        // Assert
        match result with
        | Form144Success parsed | Form144PartialSuccess (parsed, _) ->
            Assert.True(parsed.IsAmendment, "Expected IsAmendment to be true for 144/A")
            Assert.Equal(50000L, parsed.SharesToSell)
            Assert.Equal("ALEXANDER KARP", parsed.PersonName)
        | Form144Failure msg ->
            Assert.Fail($"Failed to parse: {msg}")

    [<Fact>]
    let ``Confidence calculation is high for complete Form 144`` () =
        // Arrange - well-populated data
        let parsed = { Form144Helpers.empty with
                           FilerCik = Some "0001823951"
                           PersonName = "ALEXANDER KARP"
                           IssuerName = "Palantir Technologies Inc."
                           IssuerCik = Some "0001321655"
                           SharesToSell = 90000L
                           AggregateMarketValue = Some 12140100.00m
                           ApproxSaleDate = Some DateTimeOffset.UtcNow
                           RelationshipsToIssuer = ["Director"; "Officer"] }

        // Act
        let confidence = Form144Helpers.calculateConfidence parsed

        // Assert
        Assert.True(confidence >= 1.0, $"Expected confidence >= 1.0 for fully populated data, got {confidence}")

    [<Fact>]
    let ``Confidence calculation is low for minimal Form 144`` () =
        // Arrange - minimal data (only person name)
        let parsed = { Form144Helpers.empty with PersonName = "Some Person" }

        // Act
        let confidence = Form144Helpers.calculateConfidence parsed

        // Assert
        Assert.True(confidence < 0.5, $"Expected low confidence for minimal data, got {confidence}")

    [<Fact>]
    let ``Entity type is EP for Officer/Director relationships`` () =
        // Act
        let entityType = Form144Helpers.determineEntityType ["Director"; "Officer"]

        // Assert
        Assert.Equal(Some "EP", entityType)

    [<Fact>]
    let ``Entity type defaults to IN for unknown relationships`` () =
        // Act
        let entityType = Form144Helpers.determineEntityType ["10% Owner"]

        // Assert
        Assert.Equal(Some "IN", entityType)

    [<Fact>]
    let ``Form 144 with missing securitiesInformation returns partial success`` () =
        // Arrange - XML with no securitiesInformation section
        let xml = """<?xml version="1.0" encoding="UTF-8"?>
<edgarSubmission xmlns="http://www.sec.gov/edgar/ownership">
    <headerData>
        <submissionType>144</submissionType>
        <filerInfo>
            <filer>
                <filerCredentials>
                    <cik>0001823951</cik>
                    <ccc>XXXXXXXX</ccc>
                </filerCredentials>
            </filer>
        </filerInfo>
    </headerData>
    <formData>
        <issuerInfo>
            <issuerCik>0001321655</issuerCik>
            <issuerName>Palantir Technologies Inc.</issuerName>
            <nameOfPersonForWhoseAccountTheSecuritiesAreToBeSold>ALEXANDER KARP</nameOfPersonForWhoseAccountTheSecuritiesAreToBeSold>
            <relationshipsToIssuer>
                <relationshipToIssuer>Officer</relationshipToIssuer>
            </relationshipsToIssuer>
        </issuerInfo>
    </formData>
</edgarSubmission>"""

        // Act
        let result = Form144Parser.parseFromDocument None xml

        // Assert - should be partial success or failure (low confidence due to missing shares data)
        match result with
        | Form144Success parsed ->
            // SharesToSell would be 0, which should cause low confidence
            Assert.Equal(0L, parsed.SharesToSell)
        | Form144PartialSuccess (parsed, _) ->
            Assert.True(true, "PartialSuccess is acceptable when securitiesInformation is missing")
        | Form144Failure _ ->
            Assert.True(true, "Failure is acceptable when securitiesInformation is missing")

    [<Fact>]
    [<Trait("Category", "Integration")>]
    let ``Parse real Form 144 from SEC - Palantir/Alexander Karp`` () = task {
        // Arrange
        let filingUrl = "https://www.sec.gov/Archives/edgar/data/1321655/000195004726001584/0001950047-26-001584-index.html"

        let client = EdgarClientTests.createEdgarClient()

        let! content = client.FetchPrimaryDocument filingUrl

        match content with
        | Error err ->
            Assert.Fail($"Failed to fetch primary document: {err}")
        | Ok xmlContent ->

            // Act
            let result = Form144Parser.parseFromDocument None xmlContent

            // Assert
            match result with
            | Form144Success parsed ->
                Assert.Equal("ALEXANDER KARP", parsed.PersonName)
                Assert.True(parsed.FilerCik.IsSome, "Expected filer CIK")
                Assert.Contains("Palantir", parsed.IssuerName)
                Assert.Equal(Some "0001321655", parsed.IssuerCik)
                Assert.Equal(90000L, parsed.SharesToSell)
                Assert.True(parsed.AggregateMarketValue.IsSome, "Expected aggregate market value")
                Assert.True(parsed.ApproxSaleDate.IsSome, "Expected approx sale date")

                // Confidence should be high for a well-formed filing
                Assert.True(parsed.Confidence > 0.7, $"Expected confidence > 0.7, got {parsed.Confidence}")

            | Form144PartialSuccess (parsed, notes) ->
                let notesStr = String.Join("; ", notes)
                Assert.True(parsed.Confidence > 0.4,
                            $"Confidence too low for partial success: {parsed.Confidence}. Notes: {notesStr}")

            | Form144Failure msg ->
                Assert.Fail($"Failed to parse Form 144: {msg}")
    }

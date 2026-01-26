module AlertEmailTemplateTests

open Xunit
open emailclient.EmailTemplateManager
open core.fs

[<Fact>]
let ``Alert email template renders with source lists`` () =
    // Arrange - create test data similar to what MonitoringServices produces
    let testAlertGroups = [|
        {|
            identifier = "High RSI"
            alertCount = 2
            alerts = [|
                {|
                    ticker = "AAPL"
                    value = "$175.50"
                    description = "RSI above 70 indicating overbought conditions"
                    sourceLists = [| "Tech Stocks"; "Watchlist" |]
                    sourceList = "Tech Stocks"
                    time = "10:30 ET"
                |}
                {|
                    ticker = "MSFT"
                    value = "$380.25"
                    description = "RSI above 70 indicating overbought conditions"
                    sourceLists = [| "Tech Stocks" |]
                    sourceList = "Tech Stocks"
                    time = "10:32 ET"
                |}
            |]
        |}
        {|
            identifier = "Price Breakout"
            alertCount = 1
            alerts = [|
                {|
                    ticker = "TSLA"
                    value = "$250.00"
                    description = "Price broke above resistance level"
                    sourceLists = [||] // Empty array to test the conditional
                    sourceList = ""
                    time = "11:15 ET"
                |}
            |]
        |}
    |]
    
    let testData = {|
        title = "Daily Stock Alerts"
        alertGroups = testAlertGroups
        alertDiffs = [||]
    |}
    
    // Act - render the template
    let result = processTemplate "alerts" testData |> Async.RunSynchronously
    
    // Assert
    match result with
    | Error err ->
        Assert.Fail($"Template rendering failed: {err}")
    | Ok html ->
        // Verify essential elements are present
        Assert.Contains("Daily Stock Alerts", html)
        Assert.Contains("High RSI", html)
        Assert.Contains("AAPL", html)
        Assert.Contains("MSFT", html)
        Assert.Contains("TSLA", html)
        Assert.Contains("$175.50", html)
        Assert.Contains("$380.25", html)
        Assert.Contains("$250.00", html)
        
        // Verify source lists are rendered for AAPL (has multiple lists)
        Assert.Contains("Lists:", html)
        Assert.Contains("Tech Stocks", html)
        Assert.Contains("Watchlist", html)
        
        // Verify links are present
        Assert.Contains("https://app.nightingaletrading.com/stocks/AAPL", html)
        Assert.Contains("https://www.tradingview.com/chart/kQn4rgoA/?symbol=AAPL", html)
        Assert.Contains("Dashboard", html)
        Assert.Contains("Chart", html)
        
        // Verify descriptions
        Assert.Contains("RSI above 70 indicating overbought conditions", html)
        Assert.Contains("Price broke above resistance level", html)

[<Fact>]
let ``Alert email template handles alert diffs`` () =
    // Arrange
    let testData = {|
        title = "Daily Stock Alerts"
        alertGroups = [|
            {|
                identifier = "Test Group"
                alertCount = 1
                alerts = [|
                    {|
                        ticker = "SPY"
                        value = "$450.00"
                        description = "Test alert"
                        sourceLists = [||]
                        sourceList = ""
                        time = "09:30 ET"
                    |}
                |]
            |}
        |]
        alertDiffs = [|
            {|
                identifier = "High RSI"
                previous = 5
                current = 7
                change = 2
            |}
            {|
                identifier = "Low Volume"
                previous = 3
                current = 1
                change = -2
            |}
        |]
    |}
    
    // Act
    let result = processTemplate "alerts" testData |> Async.RunSynchronously
    
    // Assert
    match result with
    | Error err ->
        Assert.Fail($"Template rendering failed: {err}")
    | Ok html ->
        Assert.Contains("Changes Since Last Alert", html)
        Assert.Contains("High RSI", html)
        Assert.Contains("+2", html)
        Assert.Contains("was 5, now 7", html)
        Assert.Contains("Low Volume", html)
        Assert.Contains("-2", html)
        Assert.Contains("was 3, now 1", html)

[<Fact>]
let ``Alert email template handles empty source lists correctly`` () =
    // Arrange - test with no source lists to ensure the conditional works
    let testData = {|
        title = "Test Alerts"
        alertGroups = [|
            {|
                identifier = "Group1"
                alertCount = 1
                alerts = [|
                    {|
                        ticker = "TEST"
                        value = "$100.00"
                        description = "Test description"
                        sourceLists = [||] // Empty array
                        sourceList = ""
                        time = "10:00 ET"
                    |}
                |]
            |}
        |]
        alertDiffs = [||]
    |}
    
    // Act
    let result = processTemplate "alerts" testData |> Async.RunSynchronously
    
    // Assert
    match result with
    | Error err ->
        Assert.Fail($"Template rendering failed: {err}")
    | Ok html ->
        // Should NOT contain "Lists:" when source lists are empty
        let listsIndex = html.IndexOf("Lists:")
        let testIndex = html.IndexOf("TEST")
        
        // If "Lists:" appears, it should not be near the TEST ticker
        if listsIndex >= 0 && testIndex >= 0 then
            let distance = abs (listsIndex - testIndex)
            Assert.True(distance > 500, "Lists: should not appear near TEST ticker when sourceLists is empty")

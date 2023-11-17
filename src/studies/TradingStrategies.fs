module studies.TradingStrategies

open core.fs.Shared.Adapters.Stocks
open studies.Types

let buyAndHoldStrategy numberOfBarsToHold (signal:GapStudyOutput.Row,prices:PriceBars) =
    // we will buy this stock at the open price of the next day
    let name =
        match numberOfBarsToHold with
        | None -> "Buy and Hold"
        | Some numberOfDaysToHold -> $"Buy and Hold {numberOfDaysToHold} bars"
        
    // find the next day
    let openDay, openDayIndex =
        match signal.Date.AddDays(1) |> prices.TryFindByDate with
        | None -> signal.Date |> prices.TryFindByDate |> Option.get // if we can't find the next day, then just open at the signal date
        | Some openDay -> openDay
    
    // find the close day
    let closeBar =
        match numberOfBarsToHold with
        | None -> prices.Last
        | Some numberOfDaysToHold ->
            let closeDayIndex = openDayIndex + numberOfDaysToHold
            match closeDayIndex with
            | x when x >= prices.Length -> prices.Last
            | _ -> prices.Bars[closeDayIndex]
    
    let openPrice = openDay.Open
    let closePrice = closeBar.Close
    
    // calculate gain percentage
    let gain = (closePrice - openPrice) / openPrice * 100m
    
    let daysHeld = closeBar.Date - openDay.Date
    
    TradeOutcomeOutput.Row(
        strategy=name,
        ticker=signal.Ticker,
        date=signal.Date,
        screenerid=signal.Screenerid,
        hasGapUp=signal.HasGapUp,
        opened=openDay.Date.DateTime,
        openPrice=openPrice,
        closed=closeBar.Date.DateTime,
        closePrice=closePrice,
        percentGain=gain,
        numberOfDaysHeld=(daysHeld.TotalDays |> int)
    )
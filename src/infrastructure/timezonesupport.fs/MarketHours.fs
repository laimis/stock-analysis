namespace timezonesupport

open System
open TimeZoneConverter
open core.fs.Adapters.Brokerage

type MarketHours() =
    
    let easternZoneId = TZConvert.GetTimeZoneInfo("Eastern Standard Time")
    let startTime = TimeSpan(9, 30, 0)
    let endTime = TimeSpan(16, 0, 0)
    
    let convertToEastern (time: DateTimeOffset) =
        TimeZoneInfo.ConvertTimeFromUtc(time.DateTime, easternZoneId)
    
    member _.IsMarketOpen(utc: DateTimeOffset) : bool =
        let eastern = TimeZoneInfo.ConvertTimeFromUtc(utc.DateTime, easternZoneId)
        
        if eastern.DayOfWeek = DayOfWeek.Saturday || eastern.DayOfWeek = DayOfWeek.Sunday then
            false
        else
            let timeOfDay = eastern.TimeOfDay
            timeOfDay >= startTime && timeOfDay <= endTime
    
    member _.ToMarketTime(utc: DateTimeOffset) : DateTimeOffset =
        DateTimeOffset(convertToEastern utc)
    
    member _.ToUniversalTime(eastern: DateTimeOffset) : DateTimeOffset =
        DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(eastern.DateTime, easternZoneId))
    
    member _.GetMarketEndOfDayTimeInUtc(eastern: DateTimeOffset) : DateTimeOffset =
        DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(eastern.Date.Add(endTime), easternZoneId))
    
    member _.GetMarketStartOfDayTimeInUtc(eastern: DateTimeOffset) : DateTimeOffset =
        DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(eastern.Date.Add(startTime), easternZoneId))
    
    interface IMarketHours with
        member this.IsMarketOpen time = this.IsMarketOpen(time)
        member this.ToMarketTime utc = this.ToMarketTime(utc)
        member this.ToUniversalTime eastern = this.ToUniversalTime(eastern)
        member this.GetMarketEndOfDayTimeInUtc eastern = this.GetMarketEndOfDayTimeInUtc(eastern)
        member this.GetMarketStartOfDayTimeInUtc eastern = this.GetMarketStartOfDayTimeInUtc(eastern)

type MarketHoursAlwaysOn() =
    
    let marketHours = MarketHours()
    
    interface IMarketHours with
        member _.GetMarketEndOfDayTimeInUtc time =
            (marketHours :> IMarketHours).GetMarketEndOfDayTimeInUtc(time)
        
        member _.GetMarketStartOfDayTimeInUtc time =
            (marketHours :> IMarketHours).GetMarketStartOfDayTimeInUtc(time)
        
        member _.IsMarketOpen _ = true
        
        member _.ToMarketTime utc =
            (marketHours :> IMarketHours).ToMarketTime(utc)
        
        member _.ToUniversalTime eastern =
            (marketHours :> IMarketHours).ToUniversalTime(eastern)

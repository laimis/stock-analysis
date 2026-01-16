namespace core.fs.Alerts

open System
open core.fs.Accounts
open core.fs
open core.Shared

type PriceAlertType =
    | PriceGoesAbove
    | PriceGoesBelow

module PriceAlertType =
    let toString = function
        | PriceGoesAbove -> "above"
        | PriceGoesBelow -> "below"
    
    let fromString = function
        | "above" -> PriceGoesAbove
        | "below" -> PriceGoesBelow
        | _ -> failwith "Invalid price alert type"

type PriceAlertState =
    | Active
    | Triggered
    | Disabled

module PriceAlertState =
    let toString = function
        | Active -> "active"
        | Triggered -> "triggered"
        | Disabled -> "disabled"
    
    let fromString = function
        | "active" -> Active
        | "triggered" -> Triggered
        | "disabled" -> Disabled
        | _ -> failwith "Invalid price alert state"

[<CLIMutable>]
type StockPriceAlert = {
    AlertId: Guid
    UserId: UserId
    Ticker: Ticker
    PriceLevel: decimal
    AlertType: PriceAlertType
    Note: string
    State: PriceAlertState
    CreatedAt: DateTimeOffset
    TriggeredAt: DateTimeOffset option
    LastResetAt: DateTimeOffset option
}

module StockPriceAlert =
    let trigger (alert: StockPriceAlert) =
        { alert with 
            State = PriceAlertState.Triggered
            TriggeredAt = Some DateTimeOffset.UtcNow 
        }
    
    let reset (alert: StockPriceAlert) =
        { alert with 
            State = PriceAlertState.Active
            TriggeredAt = None
            LastResetAt = Some DateTimeOffset.UtcNow
        }

    let create userId ticker alertType priceLevel note =
        let alertId = Guid.NewGuid()
        
        let alert: StockPriceAlert = {
            AlertId = alertId
            UserId = userId
            Ticker = ticker
            PriceLevel = priceLevel
            AlertType = alertType
            Note = note
            State = Active
            CreatedAt = DateTimeOffset.UtcNow
            TriggeredAt = None
            LastResetAt = None
        }

        alert

// DTO for API serialization (using strings instead of discriminated unions)
[<CLIMutable>]
type StockPriceAlertDto = {
    AlertId: string
    UserId: string
    Ticker: string
    PriceLevel: decimal
    AlertType: string
    Note: string
    State: string
    CreatedAt: DateTimeOffset
    TriggeredAt: DateTimeOffset option
    LastResetAt: DateTimeOffset option
}

module StockPriceAlertDto =
    let fromDomain (alert: StockPriceAlert): StockPriceAlertDto =
        let (UserId userId) = alert.UserId
        {
            AlertId = alert.AlertId.ToString()
            UserId = userId.ToString()
            Ticker = alert.Ticker.Value
            PriceLevel = alert.PriceLevel
            AlertType = PriceAlertType.toString alert.AlertType
            Note = alert.Note
            State = PriceAlertState.toString alert.State
            CreatedAt = alert.CreatedAt
            TriggeredAt = alert.TriggeredAt
            LastResetAt = alert.LastResetAt
        }

namespace core.fs.Alerts

open System
open core.fs.Accounts
open core.fs
open core.Shared

type ReminderState =
    | Pending
    | Sent
    | Dismissed

module ReminderState =
    let toString = function
        | Pending -> "pending"
        | Sent -> "sent"
        | Dismissed -> "dismissed"
    
    let fromString = function
        | "pending" -> Pending
        | "sent" -> Sent
        | "dismissed" -> Dismissed
        | _ -> failwith "Invalid reminder state"

[<CLIMutable>]
type Reminder = {
    ReminderId: Guid
    UserId: UserId
    Date: DateTimeOffset
    Message: string
    Ticker: Ticker option
    State: ReminderState
    CreatedAt: DateTimeOffset
    SentAt: DateTimeOffset option
}

module Reminder =
    let send (reminder: Reminder) =
        { reminder with 
            State = ReminderState.Sent
            SentAt = Some DateTimeOffset.UtcNow 
        }
    
    let dismiss (reminder: Reminder) =
        { reminder with 
            State = ReminderState.Dismissed
        }

    let create userId date message ticker =
        let reminderId = Guid.NewGuid()
        
        let reminder: Reminder = {
            ReminderId = reminderId
            UserId = userId
            Date = date
            Message = message
            Ticker = ticker
            State = Pending
            CreatedAt = DateTimeOffset.UtcNow
            SentAt = None
        }

        reminder

// DTO for API serialization (using strings instead of discriminated unions)
[<CLIMutable>]
type ReminderDto = {
    ReminderId: string
    UserId: string
    Date: DateTimeOffset
    Message: string
    Ticker: string option
    State: string
    CreatedAt: DateTimeOffset
    SentAt: DateTimeOffset option
}

module ReminderDto =
    let fromReminder (reminder: Reminder) : ReminderDto =
        {
            ReminderId = reminder.ReminderId.ToString()
            UserId = reminder.UserId |> IdentifierHelper.getUserId |> string
            Date = reminder.Date
            Message = reminder.Message
            Ticker = reminder.Ticker |> Option.map (fun (t:Ticker) -> t.Value)
            State = ReminderState.toString reminder.State
            CreatedAt = reminder.CreatedAt
            SentAt = reminder.SentAt
        }

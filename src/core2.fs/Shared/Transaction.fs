namespace core.Shared

open System

[<Struct>]
type Transaction =
    {
        AggregateId: Guid
        EventId: Guid
        Ticker: Ticker
        Description: string
        mutable Price: decimal
        Amount: decimal
        DateAsDate: DateTimeOffset
        IsOption: bool
        IsPL: bool
    }
    
    member this.Date = this.DateAsDate.ToString("yyyy-MM-dd")
    member this.AgeInDays = Math.Floor(DateTimeOffset.UtcNow.Subtract(this.DateAsDate).TotalDays)
    
    static member NonPLTx(aggregateId: Guid, eventId: Guid, ticker: Ticker, description: string, price: decimal, amount: decimal, ``when``: DateTimeOffset, isOption: bool) =
        {
            AggregateId = aggregateId
            EventId = eventId
            Ticker = ticker
            Description = description
            Price = price
            Amount = amount
            DateAsDate = ``when``
            IsOption = isOption
            IsPL = false
        }
    
    static member PLTx(aggregateId: Guid, ticker: Ticker, description: string, price: decimal, amount: decimal, ``when``: DateTimeOffset, isOption: bool) =
        {
            AggregateId = aggregateId
            EventId = Guid.Empty
            Ticker = ticker
            Description = description
            Price = price
            Amount = amount
            DateAsDate = ``when``
            IsOption = isOption
            IsPL = true
        }

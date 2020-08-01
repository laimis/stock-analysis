using System;
using core.Adapters.Stocks;
using core.Shared;
using MediatR;

namespace core.Notes
{
    public class NoteArchived : AggregateEvent
    {
        public NoteArchived(Guid id, Guid aggregateId, DateTimeOffset when)
         : base(id, aggregateId, when.DateTime)
        {
        }
    }

    public class NoteCreated : AggregateEvent, INotification
    {
        public NoteCreated(Guid id, Guid aggregateId, DateTimeOffset when, Guid userId, string note, string ticker)
            : base(id, aggregateId, when)
        {
            this.UserId = userId;
            this.Note = note;
            this.Ticker = ticker;
        }

        public Guid UserId { get; }
        public string Note { get; }
        public string Ticker { get; }
    }

    public class NoteEnriched : AggregateEvent
    {
        public NoteEnriched(Guid id, Guid aggregateId, DateTimeOffset when, StockAdvancedStats stats)
            : base(id, aggregateId, when)
        {
            this.Stats = stats;
        }

        public StockAdvancedStats Stats { get; }
    }

    public class NoteEnrichedWithPrice : AggregateEvent
    {
        public NoteEnrichedWithPrice(Guid id, Guid aggregateId, DateTimeOffset when, TickerPrice price, StockAdvancedStats stats)
            : base(id, aggregateId, when)
        {
            this.Price = price;
            this.Stats = stats;
        }

        public TickerPrice Price { get; }
        public StockAdvancedStats Stats { get; }
    }

    // NOTE: not used anymore, was thinking about a follow up concept
    // that turns out to be too complicated, keeping the event in to
    // make sure aggregate can be rebuilt
    public class NoteFollowedUp : AggregateEvent
    {
        public NoteFollowedUp(Guid id, Guid aggregateId, DateTimeOffset when) : base(id, aggregateId, when)
        {
        }
    }

    public class NoteReminderCleared : AggregateEvent
    {
        public NoteReminderCleared(Guid id, Guid aggregateId, DateTimeOffset when) : base(id, aggregateId, when)
        {
        }
    }

    public class NoteReminderSet : AggregateEvent
    {
        public NoteReminderSet(Guid id, Guid aggregateId, DateTimeOffset when) : base(id, aggregateId, when)
        {
        }
    }

    public class NoteUpdated : AggregateEvent
    {
        public NoteUpdated(Guid id, Guid aggregateId, DateTimeOffset when, string note)
            : base(id, aggregateId, when)
        {
            this.Note = note;
        }

        public string Note { get; private set; }
    }
}
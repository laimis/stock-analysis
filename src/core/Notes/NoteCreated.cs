using System;
using core.Shared;
using MediatR;

namespace core.Notes
{
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
}
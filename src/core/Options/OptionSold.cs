using System;
using core.Shared;
using MediatR;

namespace core.Options
{
    public class OptionSold : AggregateEvent, INotification
    {
        public OptionSold(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            Guid userId,
            int numberOfContracts,
            double premium,
            string notes)
            : base(id, aggregateId, when)
        {
            this.UserId = userId;
            this.NumberOfContracts = numberOfContracts;
            this.Premium = premium;
            this.Notes = notes;
        }

        public Guid UserId { get; }
        public int NumberOfContracts { get; }
        public double Premium { get; }
        public string Notes { get; }
    }
}
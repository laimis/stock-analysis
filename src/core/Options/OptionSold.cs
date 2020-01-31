using System;
using core.Shared;

namespace core.Options
{
    public class OptionSold : AggregateEvent
    {
        public OptionSold(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            int numberOfContracts,
            double premium)
            : base(id, aggregateId, when)
        {
            this.NumberOfContracts = numberOfContracts;
            this.Premium = premium;
        }

        public int NumberOfContracts { get; }
        public double Premium { get; }
    }
}
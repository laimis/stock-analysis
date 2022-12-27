using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Portfolio
{
    public class StockList : Aggregate
    {
        public StockListState State { get; } = new StockListState();

        public override IAggregateState AggregateState => State;

        public StockList(IEnumerable<AggregateEvent> events) : base(events)
        {
        }

        public StockList(string description, string name, Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new InvalidOperationException("Missing user id");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Missing list name");
            }

            Apply(new StockListCreated(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, description, name, userId));
        }

        public void Update(string name, string description)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Missing name");
            }

            if (State.Name == name && State.Description == description)
            {
                return;
            }

            Apply(new StockListUpdated(Guid.NewGuid(), State.Id, DateTimeOffset.UtcNow, description, name));
        }
    }

    public class StockListState : IAggregateState
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public void Apply(AggregateEvent e)
        {
            ApplyInternal(e);
        }

        protected void ApplyInternal(dynamic obj)
        {
            ApplyInternal(obj);
        }

        private void ApplyInternal(StockListCreated created)
        {
            Description = created.Description;
            Id = created.AggregateId;
            Name = created.Name;
            UserId = created.UserId;
        }

        private void ApplyInternal(StockListUpdated updated)
        {
            Description = updated.Description;
            Name = updated.Name;
        }
    }
}
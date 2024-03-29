using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Stocks
{
    public class StockListState : IAggregateState
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public List<StockListTicker> Tickers { get; } = new();
        public HashSet<string> Tags { get; } = new();

        public void Apply(AggregateEvent e) => ApplyInternal(e);

        public bool ContainsTag(string tag) => Tags.Contains(tag);

        protected void ApplyInternal(dynamic obj) => ApplyInternal(obj);

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

        private void ApplyInternal(StockListTickerAdded added) =>
            Tickers.Add(new StockListTicker(added.Note, new Ticker(added.Ticker), added.When));

        private void ApplyInternal(StockListTickerRemoved removed) =>
            Tickers.RemoveAll(x => x.Ticker.Value == removed.Ticker);

        private void ApplyInternal(StockListTagAdded added) =>
            Tags.Add(added.Tag);

        private void ApplyInternal(StockListTagRemoved removed) =>
            Tags.Remove(removed.Tag);
        
        private void ApplyInternal(StockListCleared cleared) =>
            Tickers.Clear();
    }

    public record StockListTicker(string Note, Ticker Ticker, DateTimeOffset When);
    
    public class StockList : Aggregate<StockListState>
    {
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

        public void AddStock(Ticker ticker, string note)
        {
            var exists = State.Tickers.Exists(x => x.Ticker.Equals(ticker));
            if (exists)
            {
                return;
            }
            
            Apply(new StockListTickerAdded(Guid.NewGuid(), State.Id, DateTimeOffset.UtcNow, note, ticker.Value));
        }

        public void RemoveStock(Ticker ticker)
        {
            var exists = State.Tickers.Exists(x => x.Ticker.Equals(ticker));
            if (!exists)
            {
                throw new InvalidOperationException("Ticker does not exist in the list");
            }

            Apply(new StockListTickerRemoved(Guid.NewGuid(), State.Id, DateTimeOffset.UtcNow, ticker.Value));
        }

        public void AddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                throw new InvalidOperationException("Missing tag");
            }

            if (State.ContainsTag(tag))
            {
                return;
            }

            Apply(new StockListTagAdded(Guid.NewGuid(), State.Id, DateTimeOffset.UtcNow, tag));
        }

        public void RemoveTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                throw new InvalidOperationException("Missing tag");
            }

            if (!State.ContainsTag(tag))
            {
                return;
            }

            Apply(new StockListTagRemoved(Guid.NewGuid(), State.Id, DateTimeOffset.UtcNow, tag));
        }
        
        public void Clear()
        {
            if (State.Tickers.Count == 0)
            {
                return;
            }

            Apply(new StockListCleared(Guid.NewGuid(), State.Id, DateTimeOffset.UtcNow));
        }
    }
}
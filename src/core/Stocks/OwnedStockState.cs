using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared;

namespace core.Stocks
{
    public class OwnedStockState : IAggregateState
	{
        public Guid Id { get; private set; }
        public string Ticker { get; private set; }
        public Guid UserId { get; private set; }

        public int Owned { get; private set; }
        public double Cost { get; private set; }
        public double AverageCost { get; private set; }
        public List<Transaction> Transactions { get; } = new List<Transaction>();
        internal List<IStockTransaction> BuyOrSell { get; } = new List<IStockTransaction>();
        internal HashSet<Guid> Deletes { get; } = new HashSet<Guid>();

        public List<PositionInstance> PositionInstances { get; } = new List<PositionInstance>();

        public string Description => $"{Owned} shares owned at avg cost {Math.Round(AverageCost, 2)}";

        public string Category { get; private set; }
        public int DaysHeld { get; private set; }
        public int DaysSinceLastTransaction { get; private set; }
        public IEnumerable<AggregateEvent> UndeletedBuysOrSells =>
            BuyOrSell.Where(a => Deletes.Contains(a.Id) == false).Cast<AggregateEvent>();

        internal void ApplyInternal(TickerObtained o)
        {
            Id = o.AggregateId;
            Ticker = o.Ticker;
            UserId = o.UserId;
        }

        internal void ApplyInternal(StockCategoryChanged c)
        {
            Category = c.Category;
        }

        internal void ApplyInternal(StockPurchased purchased)
        {
            BuyOrSell.Add(purchased);

            StateUpdateLoop();
        }

        internal void ApplyInternal(StockDeleted deleted)
        {
            foreach(var t in BuyOrSell)
            {
                Deletes.Add(t.Id);
            }

            StateUpdateLoop();
        }

        internal void ApplyInternal(StockTransactionDeleted deleted)
        {
            Deletes.Add(deleted.TransactionId);

            StateUpdateLoop();
        }

        internal void ApplyInternal(StockSold sold)
        {
            BuyOrSell.Add(sold);

            StateUpdateLoop();
        }

        private void StateUpdateLoop()
        {
            double avgCost = 0;
            int owned = 0;
            double cost = 0;
            var txs = new List<Transaction>();
            DateTimeOffset? oldestOpen = null;
            var positionInstances = new List<PositionInstance>();
            DateTimeOffset lastTransaction = DateTimeOffset.UtcNow;

            bool PurchaseProcessing(IStockTransaction st)
            {
                lastTransaction = st.When;

                if (owned == 0)
                {
                    oldestOpen = st.When;
                    positionInstances.Add(new PositionInstance(Ticker));
                }

                avgCost = (avgCost * owned + st.Price * st.NumberOfShares)
                        / (owned + st.NumberOfShares);
                owned += st.NumberOfShares;
                cost += st.Price * st.NumberOfShares;

                txs.Add(
                    Transaction.DebitTx(
                        Id,
                        st.Id,
                        Ticker,
                        $"Purchased {st.NumberOfShares} shares @ ${st.Price}/share",
                        st.Price * st.NumberOfShares,
                        st.When,
                        isOption: false
                    )
                );

                positionInstances[positionInstances.Count - 1].Buy(st.NumberOfShares, st.Price, st.When);

                return true;
            }

            bool SellProcessing(IStockTransaction st)
            {
                // TODO: this should never happen but in prod I see sell get
                // triggered before purchase... something is amiss
                if (positionInstances.Count > 0)
                    positionInstances[positionInstances.Count - 1].Sell(st.NumberOfShares, st.Price, st.When);

                lastTransaction = st.When;

                txs.Add(
                    Transaction.CreditTx(
                        Id,
                        st.Id,
                        Ticker,
                        $"Sold {st.NumberOfShares} shares @ ${st.Price}/share",
                        st.Price * st.NumberOfShares,
                        st.When,
                        isOption: false
                    )
                );

                txs.Add(
                    Transaction.PLTx(
                        Id,
                        Ticker,
                        $"Sold {st.NumberOfShares} shares @ ${st.Price}/share",
                        avgCost * st.NumberOfShares,
                        st.Price * st.NumberOfShares,
                        st.When,
                        isOption: false
                    )
                );
                
                owned -= st.NumberOfShares;
                cost -= avgCost * st.NumberOfShares;

                return true;
            }

            foreach(var st in BuyOrSell.OrderBy(e => e.When).ThenBy(i => BuyOrSell.IndexOf(i)))
            {
                if (Deletes.Contains(st.Id))
                {
                    continue;
                }

                if (st is StockPurchased sp)
                {
                    PurchaseProcessing(sp);
                }
                else if (st is StockSold ss)
                {
                    SellProcessing(ss);
                }
                
                if (owned == 0)
                {
                    avgCost = 0;
                    cost = 0;
                    oldestOpen = null;
                }
            }

            AverageCost = avgCost;
            Owned = owned;
            Cost = cost;
            Transactions.Clear();
            Transactions.AddRange(txs);
            PositionInstances.Clear();
            PositionInstances.AddRange(positionInstances);

            DaysHeld = oldestOpen.HasValue ? (int)Math.Floor(DateTimeOffset.UtcNow.Subtract(oldestOpen.Value).TotalDays)
                : 0;

            DaysSinceLastTransaction = (int)DateTimeOffset.UtcNow.Subtract(lastTransaction).TotalDays;
        }

        public void Apply(AggregateEvent e)
        {
            ApplyInternal(e);
        }

        protected void ApplyInternal(dynamic obj)
        {
            ApplyInternal(obj);
        }
    }

    internal interface IStockTransaction
    {
        int NumberOfShares { get; }
        double Price { get; }
        Guid Id { get; }
        DateTimeOffset When { get; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared;

namespace core.Crypto
{
    public class OwnedCryptoState : IAggregateState
	{
        public Guid Id { get; private set; }
        public string Token { get; private set; }
        public Guid UserId { get; private set; }

        public double Quantity { get; private set; }
        public double Cost { get; private set; }
        public double AverageCost { get; private set; }
        public List<CryptoTransaction> Transactions { get; } = new List<CryptoTransaction>();
        internal List<ICryptoTransaction> BuyOrSell { get; } = new List<ICryptoTransaction>();
        internal HashSet<Guid> Deletes { get; } = new HashSet<Guid>();

        public List<PositionInstance> PositionInstances { get; } = new List<PositionInstance>();

        public string Description => $"{Quantity} tokens at avg cost {Math.Round(AverageCost, 2)}";

        public int DaysHeld { get; private set; }
        public int DaysSinceLastTransaction { get; private set; }
        public IEnumerable<AggregateEvent> UndeletedBuysOrSells =>
            BuyOrSell.Where(a => Deletes.Contains(a.Id) == false).Cast<AggregateEvent>();

        internal void ApplyInternal(CryptoObtained o)
        {
            Id = o.AggregateId;
            Token = o.Token;
            UserId = o.UserId;
        }

        internal void ApplyInternal(CryptoPurchased purchased)
        {
            BuyOrSell.Add(purchased);

            StateUpdateLoop();
        }

        internal void ApplyInternal(CryptoDeleted deleted)
        {
            foreach(var t in BuyOrSell)
            {
                Deletes.Add(t.Id);
            }

            StateUpdateLoop();
        }

        internal void ApplyInternal(CryptoTransactionDeleted deleted)
        {
            Deletes.Add(deleted.TransactionId);

            StateUpdateLoop();
        }

        internal void ApplyInternal(CryptoSold sold)
        {
            BuyOrSell.Add(sold);

            StateUpdateLoop();
        }

        private void StateUpdateLoop()
        {
            double avgCost = 0;
            double quantity = 0;
            double cost = 0;
            var txs = new List<CryptoTransaction>();
            DateTimeOffset? oldestOpen = null;
            var positionInstances = new List<PositionInstance>();
            DateTimeOffset lastTransaction = DateTimeOffset.UtcNow;

            bool PurchaseProcessing(ICryptoTransaction st)
            {
                lastTransaction = st.When;

                if (quantity == 0)
                {
                    oldestOpen = st.When;
                    positionInstances.Add(new PositionInstance(Token));
                }

                quantity += st.Quantity;
                cost += st.DollarAmount;

                txs.Add(
                    CryptoTransaction.DebitTx(
                        Id,
                        st.Id,
                        Token,
                        $"Purchased {st.Quantity} for ${st.DollarAmount}",
                        st.DollarAmount,
                        st.When
                    )
                );

                positionInstances[positionInstances.Count - 1].Buy(st.Quantity, st.DollarAmount, st.When);

                return true;
            }

            bool SellProcessing(ICryptoTransaction st)
            {
                // TODO: this should never happen but in prod I see sell get
                // triggered before purchase... something is amiss
                if (positionInstances.Count > 0)
                    positionInstances[positionInstances.Count - 1].Sell(st.Quantity, st.DollarAmount, st.When);

                lastTransaction = st.When;

                txs.Add(
                    CryptoTransaction.CreditTx(
                        Id,
                        st.Id,
                        Token,
                        $"Sold {st.Quantity} for ${st.DollarAmount}",
                        st.DollarAmount,
                        st.When
                    )
                );
                
                quantity -= st.Quantity;
                cost -= st.DollarAmount;

                return true;
            }

            foreach(var st in BuyOrSell.OrderBy(e => e.When).ThenBy(i => BuyOrSell.IndexOf(i)))
            {
                if (Deletes.Contains(st.Id))
                {
                    continue;
                }

                if (st is CryptoPurchased sp)
                {
                    PurchaseProcessing(sp);
                }
                else if (st is CryptoSold ss)
                {
                    SellProcessing(ss);
                }
                
                if (quantity == 0)
                {
                    avgCost = 0;
                    cost = 0;
                    oldestOpen = null;
                }
            }

            AverageCost = avgCost;
            Quantity = quantity;
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
}
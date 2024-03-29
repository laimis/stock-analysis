using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared;

namespace core.Cryptos
{
    public class OwnedCryptoState : IAggregateState
	{
        public Guid Id { get; private set; }
        public string Token { get; private set; }
        public Guid UserId { get; private set; }

        public decimal Quantity { get; private set; }
        public decimal Cost { get; private set; }
        public decimal AverageCost => Cost / Quantity;
        public List<CryptoTransaction> Transactions { get; } = new();
        internal List<ICryptoTransaction> BuyOrSell { get; } = new();
        internal HashSet<Guid> Deletes { get; } = new();

        public List<PositionInstance> PositionInstances { get; } = new();

        public string Description => $"{Quantity} tokens at avg cost {Math.Round(AverageCost, 2)}";

        public int DaysHeld { get; private set; }
        public int DaysSinceLastTransaction { get; private set; }
        public IEnumerable<AggregateEvent> UndeletedBuysOrSells =>
            BuyOrSell.Where(a => Deletes.Contains(a.Id) == false).Cast<AggregateEvent>();

        public List<CryptoAwarded> Awards { get; } = new List<CryptoAwarded>();
        public List<CryptoYielded> Yields { get; } = new List<CryptoYielded>();

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

        internal void ApplyInternal(CryptoAwarded awarded)
        {
            Awards.Add(awarded);

            StateUpdateLoop();
        }

        internal void ApplyInternal(CryptoYielded yielded)
        {
            Yields.Add(yielded);

            StateUpdateLoop();
        }

        private void StateUpdateLoop()
        {
            decimal quantity = 0;
            decimal cost = 0;
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
                        st.DollarAmount / st.Quantity,
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
                        st.DollarAmount / st.Quantity,
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
                    cost = 0;
                    oldestOpen = null;
                }
            }

            foreach(var a in Awards)
            {
                if (Deletes.Contains(a.Id))
                {
                    continue;
                }

                if (quantity == 0)
                {
                    oldestOpen = a.When;
                }

                quantity += a.Quantity;
            }

            foreach(var y in Yields)
            {
                if (Deletes.Contains(y.Id))
                {
                    continue;
                }

                quantity += y.Quantity;
            }

            Quantity = quantity;
            Cost = cost;
            Transactions.Clear();
            Transactions.AddRange(txs);
            PositionInstances.Clear();
            PositionInstances.AddRange(positionInstances);

            DaysHeld = oldestOpen.HasValue ? 
                (int)Math.Floor(DateTimeOffset.UtcNow.Subtract(oldestOpen.Value).TotalDays) : 0;

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
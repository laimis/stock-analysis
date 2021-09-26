using System;
using System.Collections.Generic;
using System.Linq;

namespace core.Cryptos.Handlers
{
    public class CoinbaseProContainer
    {
        public List<TransactionGroup> Transactions { get; private set; }

        public void AddRecords(IEnumerable<CoinbaseProRecord> records)
        {
            Transactions = records
                .Where(r => r.Type == "match")
                .GroupBy(r => r.TradeId)
                .Select(g => new TransactionGroup(g))
                .ToList();
        }

        public IEnumerable<TransactionGroup> GetBuys() => Transactions.Where(t => t.IsBuy);
        public IEnumerable<TransactionGroup> GetSells() => Transactions.Where(t => t.IsSell);

        public class TransactionGroup
        {
            public TransactionGroup(IGrouping<string, CoinbaseProRecord> group)
            {
                var records = group.ToList();

                if (records.Count > 2)
                {
                    throw new InvalidOperationException("More than two records found for " + records[0].TradeId);
                }

                IsBuy = records[0].AmountBalanceUnit == "USD";
                
                DollarAmount = IsBuy switch {
                    true => Math.Abs(records[0].Amount.Value),
                    false => records[1].Amount.Value
                };

                Quantity = IsBuy switch {
                    true => records[1].Amount.Value,
                    false => Math.Abs(records[0].Amount.Value)
                };

                Token = IsBuy switch {
                    true => records[1].AmountBalanceUnit,
                    false => records[0].AmountBalanceUnit
                };

                Date = records[0].Time;
            }

            public bool IsBuy { get; }
            public bool IsSell => !IsBuy;
            public decimal DollarAmount { get; }
            public decimal Quantity { get; }
            public string Token { get; }
            public DateTimeOffset Date { get; }
        }
    }
}
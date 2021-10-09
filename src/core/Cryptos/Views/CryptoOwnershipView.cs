using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared;

namespace core.Cryptos.Views
{
    public class CryptoOwnershipView
    {
        public CryptoOwnershipView(OwnedCryptoState state)
        {
            Id = state.Id;
            AverageCost = state.AverageCost;
            Cost = state.Cost;
            Quantity = state.Quantity;
            Token = state.Token;
            Transactions = state.Transactions
                .Select(t => t.ToSharedTransaction())
                .OrderByDescending(t => t.Date)
                .ToList();
        }

        public Guid Id { get; }
        public decimal AverageCost { get; }
        public decimal Cost { get; }
        public decimal Quantity { get; }
        public string Token { get; }
        public List<Transaction> Transactions { get; }
    }
}
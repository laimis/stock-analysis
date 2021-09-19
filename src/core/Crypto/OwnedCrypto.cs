using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared;

namespace core.Crypto
{
    public class OwnedCrypto : Aggregate
    {
        public OwnedCryptoState State { get; } = new OwnedCryptoState();
        public override IAggregateState AggregateState => State;
        
        public OwnedCrypto(IEnumerable<AggregateEvent> events) : base(events)
        {
        }

        public OwnedCrypto(Token token, Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new InvalidOperationException("Missing user id");
            }

            Apply(new CryptoObtained(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, token, userId));
        }

        public void Purchase(double quantity, double dollarAmountSpent, DateTimeOffset date, string notes = null)
        {
            if (quantity <= 0)
            {
                throw new InvalidOperationException("Price cannot be empty or zero");
            }

            if (dollarAmountSpent <= 0)
            {
                throw new InvalidOperationException("Price cannot be empty or zero");
            }

            if (date == DateTime.MinValue)
            {
                throw new InvalidOperationException("Purchase date not specified");
            }

            Apply(
                new CryptoPurchased(
                    Guid.NewGuid(),
                    State.Id,
                    date,
                    State.UserId,
                    State.Token,
                    quantity,
                    dollarAmountSpent,
                    notes
                )
            );
        }

        public void DeleteTransaction(Guid transactionId)
        {
            if (!State.BuyOrSell.Any(t => t.Id == transactionId))
            {
                throw new InvalidOperationException("Unable to find transcation to delete using id " + transactionId);
            }

            Apply(
                new CryptoTransactionDeleted(
                    Guid.NewGuid(),
                    State.Id,
                    transactionId,
                    DateTimeOffset.UtcNow
                    
                )
            );
        }

        public void Delete()
        {
            Apply(
                new CryptoDeleted(
                    Guid.NewGuid(),
                    State.Id,
                    DateTimeOffset.UtcNow
                )
            );
        }

        public void Sell(int quantity, double dollarAmountReceived, DateTimeOffset date, string notes)
        {
            if (quantity > State.Quantity)
            {
                throw new InvalidOperationException("Amount owned is less than what is desired to sell");
            }

            Apply(
                new CryptoSold(
                    Guid.NewGuid(),
                    State.Id,
                    date,
                    State.UserId,
                    State.Token,
                    quantity,
                    dollarAmountReceived,
                    notes)
            );
        }
    }
}
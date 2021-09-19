using System;
using core.Cryptos;
using Xunit;

namespace coretests.Crypto
{
    public class OwnedCryptoTests
    {
        [Fact]
        public void Btc_EndToEnd()
        {
            var btc = new OwnedCrypto(
                new Token("BTC"), System.Guid.NewGuid()
            );

            btc.Purchase(
                quantity: 0.05437703,
                dollarAmountSpent: 985.32,
                date: System.DateTimeOffset.Parse("2017-12-19T15:05:38Z")
            );

            Assert.Equal(0.05437703, btc.State.Quantity);
            Assert.Equal(985.32, btc.State.Cost);

            btc.Sell(0.004, dollarAmountReceived: 74, DateTimeOffset.UtcNow, notes: null);

            Assert.Equal(0.05037703, btc.State.Quantity);

            Assert.Equal(2, btc.State.Transactions.Count);
            Assert.Single(btc.State.PositionInstances);
        }
    }
}
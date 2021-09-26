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
                quantity: 0.05437703m,
                dollarAmountSpent: 985.32m,
                date: System.DateTimeOffset.Parse("2017-12-19T15:05:38Z")
            );

            Assert.Equal(0.05437703m, btc.State.Quantity);
            Assert.Equal(985.32m, btc.State.Cost);

            btc.Sell(0.004m, dollarAmountReceived: 74, DateTimeOffset.UtcNow, notes: null);

            Assert.Equal(0.05037703m, btc.State.Quantity);

            Assert.Equal(2, btc.State.Transactions.Count);
            Assert.Single(btc.State.PositionInstances);
        }

        [Fact]
        public void Airdrops()
        {
            var xlm = new OwnedCrypto(
                new Token("XLM"), System.Guid.NewGuid()
            );

            xlm.Reward(quantity: 0.012m,
                       dollarAmountWorth: 2.0m,
                       date: DateTimeOffset.UtcNow,
                       notes: "Received 4.9954541 XLM from Coinbase Earn");

            Assert.Equal("XLM", xlm.State.Token);
            Assert.Equal(0.012m, xlm.State.Quantity);
            Assert.Equal(0, xlm.State.Cost);
            Assert.Empty(xlm.State.PositionInstances);
            Assert.Empty(xlm.State.Transactions);
        }

        [Fact]
        public void Yields()
        {
            var algo = new OwnedCrypto(
                new Token("ALGO"), System.Guid.NewGuid()
            );

            algo.Yield(quantity: 0.080861m,
                       dollarAmountWorth: 0.11m,
                       date: DateTimeOffset.UtcNow,
                       notes: "Received 0.080861 ALGO from Coinbase Rewards");

            Assert.Equal("ALGO", algo.State.Token);
            Assert.Equal(0.080861m, algo.State.Quantity);
            Assert.Equal(0, algo.State.Cost);
            Assert.Empty(algo.State.PositionInstances);
            Assert.Empty(algo.State.Transactions);
        }
    }
}
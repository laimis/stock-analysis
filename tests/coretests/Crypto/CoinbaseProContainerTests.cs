using System.Linq;
using core.fs.Cryptos.Import;
using Xunit;

namespace coretests.Crypto
{
    public class CoinbaseProContainerTests
    {
        [Fact]
        public void EndToEnd()
        {
            var records = new [] {
                // buying algo
                new CoinbaseProRecord(type: "match", time: System.DateTimeOffset.Parse("2021-04-17T13:20:46.608Z"), tradeId: "18726758", amount: -462, amountBalanceUnit: "USD", orderId: ""),
                new CoinbaseProRecord(type: "match", time: System.DateTimeOffset.Parse("2021-04-17T13:20:46.608Z"), tradeId: "18726758", amount: 300, amountBalanceUnit: "ALGO", orderId: ""),
                // buying link
                new CoinbaseProRecord(type: "match", time: System.DateTimeOffset.Parse("2021-04-17T13:20:46.608Z"), tradeId: "1", amount: -174.7397855m, amountBalanceUnit: "USD", orderId: ""),
                new CoinbaseProRecord(type: "match", time: System.DateTimeOffset.Parse("2021-04-17T13:20:46.608Z"), tradeId: "1", amount: 10.51m, amountBalanceUnit: "LINK", orderId: ""),
                // selling link 1
                new CoinbaseProRecord(type: "match", time: System.DateTimeOffset.Parse("2021-04-17T13:20:46.608Z"), tradeId: "2", amount: -9.1m, amountBalanceUnit: "LINK", orderId: ""),
                new CoinbaseProRecord(type: "match", time: System.DateTimeOffset.Parse("2021-04-17T13:20:46.608Z"), tradeId: "2", amount: 163.837037m, amountBalanceUnit: "USD", orderId: ""),
            };

            var container = new CoinbaseProContainer(records);

            Assert.NotEmpty(container.Transactions);
            
            var buy = container.Buys.First();

            Assert.Equal("ALGO", buy.Token);
            Assert.Equal(300, buy.Quantity);
            Assert.Equal(462, buy.DollarAmount);
            Assert.Equal(2021, buy.Date.Year);

            var sell = container.Sells.First();

            Assert.Equal("LINK", sell.Token);
            Assert.Equal(9.1m, sell.Quantity);
            Assert.Equal(163.837037m, sell.DollarAmount);
            Assert.Equal(2021, sell.Date.Year);
        }
    }
}
using System.Linq;
using core.Cryptos.Handlers;
using Xunit;

namespace coretests.Crypto
{
    public class CoinbaseProContainerTests
    {
        [Fact]
        public void EndToEnd()
        {
            var container = new CoinbaseProContainer();

            var records = new CoinbaseProRecord[] {
                // buying algo
                new CoinbaseProRecord {
                    Amount = -462,
                    AmountBalanceUnit = "USD",
                    Time = System.DateTimeOffset.Parse("2021-04-17T13:20:46.608Z"),
                    TradeId = "18726758",
                    Type = "match"
                },
                new CoinbaseProRecord {
                    Amount = 300,
                    AmountBalanceUnit = "ALGO",
                    Time = System.DateTimeOffset.Parse("2021-04-17T13:20:46.608Z"),
                    TradeId = "18726758",
                    Type = "match"
                },
                // buying link
                new CoinbaseProRecord {
                    Amount = -174.7397855m,
                    AmountBalanceUnit = "USD",
                    Time = System.DateTimeOffset.Parse("2021-04-17T13:20:46.608Z"),
                    TradeId = "1",
                    Type = "match"
                },
                new CoinbaseProRecord {
                    Amount = 10.51m,
                    AmountBalanceUnit = "LINK",
                    Time = System.DateTimeOffset.Parse("2021-04-17T13:20:46.608Z"),
                    TradeId = "1",
                    Type = "match"
                },
                // selling link 1
                new CoinbaseProRecord {
                    Amount = -9.1m,
                    AmountBalanceUnit = "LINK",
                    Time = System.DateTimeOffset.Parse("2021-04-17T13:20:46.608Z"),
                    TradeId = "2",
                    Type = "match"
                },
                new CoinbaseProRecord {
                    Amount = 163.837037m,
                    AmountBalanceUnit = "USD",
                    Time = System.DateTimeOffset.Parse("2021-04-17T13:20:46.608Z"),
                    TradeId = "2",
                    Type = "match"
                },
            };

            container.AddRecords(records);

            Assert.NotEmpty(container.Transactions);
            
            var buy = container.GetBuys().First();

            Assert.Equal("ALGO", buy.Token);
            Assert.Equal(300, buy.Quantity);
            Assert.Equal(462, buy.DollarAmount);
            Assert.Equal(2021, buy.Date.Year);

            var sell = container.GetSells().First();

            Assert.Equal("LINK", sell.Token);
            Assert.Equal(9.1m, sell.Quantity);
            Assert.Equal(163.837037m, sell.DollarAmount);
            Assert.Equal(2021, sell.Date.Year);
        }
    }
}
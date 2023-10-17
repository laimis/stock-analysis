using System;
using System.Collections.Generic;
using core.Shared;
using core.Stocks;
using coretests.testdata;

namespace coretests.Stocks
{
    public class TradingDataGenerator
    {
        internal static PositionInstance[] GetClosedPositions()
        {
            var closedPositions = new List<PositionInstance>();

            var position = new PositionInstance(0, TestDataGenerator.TSLA, DateTimeOffset.Now.AddDays(-1));
            position.Buy(1, 100m, DateTimeOffset.Now.AddDays(-1), transactionId: Guid.NewGuid());
            position.Sell(1, 110m, transactionId: Guid.NewGuid(), DateTimeOffset.Now);
            closedPositions.Add(position);

            position = new PositionInstance(1, TestDataGenerator.TSLA, DateTimeOffset.Now.AddDays(-1));
            position.Buy(1, 100m, DateTimeOffset.Now.AddDays(-1), transactionId: Guid.NewGuid());
            position.Sell(1, 110m, transactionId: Guid.NewGuid(), DateTimeOffset.Now);
            closedPositions.Add(position);

            position = new PositionInstance(2, TestDataGenerator.TSLA, DateTimeOffset.Now.AddDays(-1));
            position.Buy(1, 100m, DateTimeOffset.Now.AddDays(-1), transactionId: Guid.NewGuid());
            position.Sell(1, 90m, transactionId: Guid.NewGuid(), DateTimeOffset.Now);
            closedPositions.Add(position);

            return closedPositions.ToArray();
        }

        internal static PositionInstance[] GenerateRandomSet(
            DateTimeOffset start,
            int minimumNumberOfTrades)
        {
            var closedPositions = new List<PositionInstance>();
            
            var random = new Random();
            var numberOfStocks = random.Next(minimumNumberOfTrades, minimumNumberOfTrades + 100);
            
            for(var i = 0; i < numberOfStocks; i++)
            {
                var stock = GenerateRandomTicker(random);
                var date = start.AddDays(random.Next(100));
                var shares = random.Next(1, 100);
                var price = random.Next(1, 1000);
                var days = random.Next(1, 100);
                var sellPrice = random.Next(1, 1000);
                
                var position = new PositionInstance(i, stock, date);
                position.Buy(shares, price, date, transactionId: Guid.NewGuid());
                position.Sell(shares, sellPrice, transactionId: Guid.NewGuid(), date.AddDays(days));
                closedPositions.Add(position);
            }
            
            return closedPositions.ToArray();
        }
        
        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static Ticker GenerateRandomTicker(Random random)
        {
            var length = random.Next(3, 5);
            var ticker = new char[length];
            for(var i = 0; i < length; i++)
            {
                ticker[i] = Chars[random.Next(Chars.Length)];
            }
            return new Ticker(ticker.ToString());
        }
    }
}
using System;
using System.Linq;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services.Trading
{
    internal class TradingStrategyWithDownsideProtection : TradingStrategy
    {
        private int _downsideProtectionSize;
        private bool _executed;

        internal TradingStrategyWithDownsideProtection(
            bool closeIfOpenAtTheEnd,
            string name,
            int numberOfProfitPoints,
            Func<PositionInstance, int, decimal> getProfitPoint,
            Func<PositionInstance, int, decimal> getStopPrice,
            int downsideProtectionSize) : base(closeIfOpenAtTheEnd, name, numberOfProfitPoints, getProfitPoint, getStopPrice)
        {
            _downsideProtectionSize = downsideProtectionSize;
            _executed = false;
        }

        protected override SimulationContext ApplyPriceBarToPosition(SimulationContext context, PriceBar bar)
        {
            var appliedContext = base.ApplyPriceBarToPosition(context, bar);

            if (!_executed && appliedContext.Position.RR < -0.5m && appliedContext.Position.NumberOfShares > 0)
            {
                var stocksToSell = (int)(appliedContext.Position.NumberOfShares / _downsideProtectionSize);
                if (stocksToSell > 0)
                {
                    appliedContext.Position.Sell(stocksToSell, bar.Close, Guid.NewGuid(), bar.Date);
                    _executed = true;
                }
            }

            return appliedContext;
        }
    }

    internal class TradingStrategy : ITradingStrategy
    {
        internal TradingStrategy(
            bool closeIfOpenAtTheEnd,
            string name,
            int numberOfProfitPoints,
            Func<PositionInstance, int, decimal> getProfitPoint,
            Func<PositionInstance, int, decimal> getStopPrice
        )
        {
            CloseIfOpenAtTheEnd = closeIfOpenAtTheEnd;
            Name = name;
            NumberOfProfitPoints = numberOfProfitPoints;
            ProfitPointFunc = getProfitPoint;
            StopPriceFunc = getStopPrice;
            
            _level = 1;
        }

        public bool CloseIfOpenAtTheEnd { get; }
        public string Name { get; }
        public int NumberOfProfitPoints { get; }
        public Func<PositionInstance, int, decimal> ProfitPointFunc { get; }
        public Func<PositionInstance, int, decimal> StopPriceFunc { get; }

        private int _level;
        private decimal _numberOfSharesAtStart;

        public TradingStrategyResult Run(PositionInstance position, PriceBar[] bars)
        {
            _numberOfSharesAtStart = position.NumberOfShares;

            var context = new SimulationContext(
                Position: position,
                MaxDrawdown: 0,
                MaxGain: 0
            );
            
            var finalContext = bars.Aggregate(context, ApplyPriceBarToPosition);

            var finalPosition = finalContext.Position;

            if (CloseIfOpenAtTheEnd && !finalPosition.IsClosed)
            {
                ClosePosition(bars[^1], finalPosition);
            }

            return new TradingStrategyResult(
                maxDrawdownPct: finalContext.MaxDrawdown,
                maxGainPct: finalContext.MaxGain,
                position: finalPosition,
                strategyName: Name
            );
        }

        private static void ClosePosition(PriceBar bar, PositionInstance position)
        {
            position.Sell(
                numberOfShares: position.NumberOfShares,
                price: bar.Close,
                transactionId: Guid.NewGuid(),
                when: bar.Date
            );
        }

        protected virtual SimulationContext ApplyPriceBarToPosition(SimulationContext context, PriceBar bar)
        {
            var position = context.Position;

            if (position.IsClosed)
            {
                return context;
            }

            var sellPrice = ProfitPointFunc(position, _level);
            if (bar.High >= sellPrice)
            {
                ExecuteProfitSell(position, sellPrice, bar);

                _level++;
            }

            // if stop is reached, sell at the close price
            if (bar.Close <= position.StopPrice.Value)
            {
                ClosePosition(bar, position);
            }

            position.SetPrice(bar.Close);

            return context with {
                Position = position,
                MaxDrawdown = Math.Min(context.MaxDrawdown, bar.PercentDifferenceFromLow(position.AverageBuyCostPerShare)),
                MaxGain = Math.Max(context.MaxGain, bar.PercentDifferenceFromHigh(position.AverageBuyCostPerShare))
            };
        }

        private void ExecuteProfitSell(PositionInstance position, decimal sellPrice, PriceBar bar)
        {
            var portion = (int)(_numberOfSharesAtStart / NumberOfProfitPoints);
            if (portion == 0)
            {
                portion = 1;
            }

            if (position.NumberOfShares < portion)
            {
                portion = (int)position.NumberOfShares;
            }

            if (_level == NumberOfProfitPoints)
            {
                // sell all the remaining shares
                portion = (int)position.NumberOfShares;
            }

            position.Sell(
                numberOfShares: portion, 
                price: sellPrice,
                transactionId: Guid.NewGuid(),
                when: bar.Date
            );

            if (position.NumberOfShares > 0)
            {
                position.SetStopPrice(
                    StopPriceFunc(position, _level),
                    bar.Date
                );
            }
        }
    }
}
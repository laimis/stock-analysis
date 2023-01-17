using System;
using System.Linq;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services.Trading
{
    internal class TradingStrategyCloseOnCondition : TradingStrategy
    {
        private Func<SimulationContext, PriceBar, bool> _condition;

        public TradingStrategyCloseOnCondition(
            string name,
            Func<SimulationContext, PriceBar, bool> condition) : base(name) =>

            _condition = condition;

        protected override void ApplyPriceBarToPositionInternal(SimulationContext context, PriceBar bar)
        {
            if (context.Position.NumberOfShares > 0 && _condition(context, bar))
            {
                ClosePosition(bar.Close, bar.Date, context.Position);
            }
        }
    }

    internal class TradingStrategyWithDownsideProtection : TradingStrategyWithProfitPoints
    {
        private int _downsideProtectionSize;
        private bool _executed;

        internal TradingStrategyWithDownsideProtection(
            string name,
            int numberOfProfitPoints,
            Func<PositionInstance, int, decimal> getProfitPoint,
            Func<PositionInstance, int, decimal> getStopPrice,
            int downsideProtectionSize) : base(name, numberOfProfitPoints, getProfitPoint, getStopPrice)
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

    internal class TradingStrategyWithProfitPoints : TradingStrategy
    {
        private int _numberOfProfitPoints;
        private Func<PositionInstance, int, decimal> _profitPointFunc;
        private Func<PositionInstance, int, decimal> _stopPriceFunc;
        private int _level;
        private bool _useLowAsStop;

        internal TradingStrategyWithProfitPoints(
            string name,
            int numberOfProfitPoints,
            Func<PositionInstance, int, decimal> getProfitPoint,
            Func<PositionInstance, int, decimal> getStopPoint,
            bool useLowAsStop = false
        ) : base(name)
        {
            _numberOfProfitPoints = numberOfProfitPoints;
            _profitPointFunc = getProfitPoint;
            _stopPriceFunc = getStopPoint;
            _level = 1;
            _useLowAsStop = useLowAsStop;
        }

        protected override void ApplyPriceBarToPositionInternal(SimulationContext context, PriceBar bar)
        {
            var sellPrice = _profitPointFunc(context.Position, _level);
            if (bar.High >= sellPrice)
            {
                ExecuteProfitSell(context.Position, sellPrice, bar);
                _level++;
            }

            // if stop is reached, sell at the close price
            var stopThreshold = _useLowAsStop ? bar.Low : bar.Close;
            if (stopThreshold <= context.Position.StopPrice.Value)
            {
                // if we are using low as a stop, then our sale price will be
                // our stop price, or if there is a gap down, a high of that day that's
                // below the stop price
                var sellingPrice = _useLowAsStop switch {
                    true => Math.Min(bar.High, context.Position.StopPrice.Value),
                    false => bar.Close
                };

                ClosePosition(context.Position.StopPrice.Value, bar.Date, context.Position);
            }
        }

        private void ExecuteProfitSell(PositionInstance position, decimal sellPrice, PriceBar bar)
        {
            var portion = (int)(_numberOfSharesAtStart / _numberOfProfitPoints);
            if (portion == 0)
            {
                portion = 1;
            }

            if (position.NumberOfShares < portion)
            {
                portion = (int)position.NumberOfShares;
            }

            if (_level == _numberOfProfitPoints)
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
                    _stopPriceFunc(position, _level),
                    bar.Date
                );
            }
        }
    }
    
    internal abstract class TradingStrategy : ITradingStrategy
    {
        internal TradingStrategy(string name)
        {
            Name = name;
        }

        public string Name { get; }
        protected decimal _numberOfSharesAtStart;

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

            return new TradingStrategyResult(
                maxDrawdownPct: finalContext.MaxDrawdown,
                maxGainPct: finalContext.MaxGain,
                position: finalPosition,
                strategyName: Name
            );
        }

        protected static void ClosePosition(decimal price, DateTimeOffset date, PositionInstance position)
        {
            position.Sell(
                numberOfShares: position.NumberOfShares,
                price: price,
                transactionId: Guid.NewGuid(),
                when: date
            );
        }

        protected virtual SimulationContext ApplyPriceBarToPosition(SimulationContext context, PriceBar bar)
        {
            var position = context.Position;

            if (position.IsClosed)
            {
                return context;
            }

            ApplyPriceBarToPositionInternal(context, bar);

            position.SetPrice(bar.Close);

            return context with {
                Position = position,
                MaxDrawdown = Math.Min(context.MaxDrawdown, bar.PercentDifferenceFromLow(position.AverageBuyCostPerShare)),
                MaxGain = Math.Max(context.MaxGain, bar.PercentDifferenceFromHigh(position.AverageBuyCostPerShare))
            };
        }

        protected abstract void ApplyPriceBarToPositionInternal(SimulationContext context, PriceBar bar);
    }
}
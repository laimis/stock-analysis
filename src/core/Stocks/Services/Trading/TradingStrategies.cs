using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services.Trading
{
    internal class TradingStrategyCloseOnCondition : TradingStrategy
    {
        private Func<SimulationContext, PriceBar, bool> _exitCondition;

        public TradingStrategyCloseOnCondition(
            string name,
            Func<SimulationContext, PriceBar, bool> exitCondition) : base(name) =>

            _exitCondition = exitCondition;

        protected override void ApplyPriceBarToPositionInternal(SimulationContext context, PriceBar bar)
        {
            if (context.Position.NumberOfShares > 0 && _exitCondition(context, bar))
            {
                ClosePosition(bar.Close, bar.Date, context.Position);
            }
        }
    }

    internal class TradingStrategyActualTrade : ITradingStrategy
    {
        public TradingStrategyActualTrade() { }

        public TradingStrategyResult Run(PositionInstance position, IEnumerable<PriceBar> bars)
        {
            var maxDrawdownPct = 0m;
            var maxGainPct = 0m;
            var last10Bars = new List<PriceBar>(10);

            foreach (var bar in bars)
            {
                position.SetPrice(bar.Close);

                maxDrawdownPct = Math.Min(maxDrawdownPct, bar.PercentDifferenceFromLow(position.AverageBuyCostPerShare));
                maxGainPct = Math.Max(maxGainPct, bar.PercentDifferenceFromHigh(position.AverageBuyCostPerShare));

                if (last10Bars.Count == 10)
                {
                    last10Bars.RemoveAt(0);
                }
                last10Bars.Add(bar);

                if (position.IsClosed)
                {
                    if (bar.Date.Date == position.Closed.Value.Date)
                    {
                        break;
                    }
                }
            }

            var (maxDrawdownPctRecent, maxGainPctRecent) = 
                TradingStrategy.CalculateMaxDrawdownAndGain(last10Bars);

            return new TradingStrategyResult(
                maxDrawdownPct: maxDrawdownPct,
                maxGainPct: maxGainPct,
                maxDrawdownPctRecent: maxDrawdownPctRecent,
                maxGainPctRecent: maxGainPctRecent,
                position: position,
                strategyName: "Actual trade ⭐"
            );
        }
    }

    internal class TradingStrategyWithAdvancingStops : TradingStrategy
    {
        private Func<PositionInstance, int, decimal> _stopPriceFunc;
        private Func<PositionInstance, int, decimal>  _profitPointFunc;
        private int _level = 1;
        
        internal TradingStrategyWithAdvancingStops(
            string name,
            Func<PositionInstance, int, decimal> profitPointFunc,
            Func<PositionInstance, int, decimal> stopPriceFunc) : base(name) =>
            (_profitPointFunc, _stopPriceFunc) = (profitPointFunc, stopPriceFunc);

        protected override void ApplyPriceBarToPositionInternal(SimulationContext context, PriceBar bar)
        {
            var profitPoint = _profitPointFunc(context.Position, _level);
            if (bar.High > profitPoint)
            {
                context.Position.SetStopPrice(_stopPriceFunc(context.Position, _level), bar.Date);
                _level++;
            }

            if (bar.Close <= context.Position.StopPrice.Value)
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

        internal TradingStrategyWithProfitPoints(
            string name,
            int numberOfProfitPoints,
            Func<PositionInstance, int, decimal> getProfitPoint,
            Func<PositionInstance, int, decimal> getStopPoint
        ) : base(name)
        {
            _numberOfProfitPoints = numberOfProfitPoints;
            _profitPointFunc = getProfitPoint;
            _stopPriceFunc = getStopPoint;
            _level = 1;
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
            if (bar.Close <= context.Position.StopPrice.Value)
            {
                ClosePosition(bar.Close, bar.Date, context.Position);
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

        public TradingStrategyResult Run(PositionInstance position, IEnumerable<PriceBar> bars)
        {
            _numberOfSharesAtStart = position.NumberOfShares;

            var context = new SimulationContext(
                Position: position,
                MaxDrawdown: 0,
                MaxGain: 0,
                Last10Bars: new List<PriceBar>(10)
            );
            
            var finalContext = bars.Aggregate(context, ApplyPriceBarToPosition);

            var finalPosition = finalContext.Position;

            var (maxDrawdownPctRecent, maxGainPctRecent) = TradingStrategy.CalculateMaxDrawdownAndGain(
                finalContext.Last10Bars
            );

            return new TradingStrategyResult(
                maxDrawdownPct: finalContext.MaxDrawdown,
                maxGainPct: finalContext.MaxGain,
                maxDrawdownPctRecent: maxDrawdownPctRecent,
                maxGainPctRecent: maxGainPctRecent,
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

            var last10bars = context.Last10Bars;
            // move the last 10 bars forward
            if (last10bars.Count == 10)
            {
                last10bars.RemoveAt(0);
            }
            last10bars.Add(bar);

            return context with {
                Position = position,
                MaxDrawdown = Math.Min(context.MaxDrawdown, bar.PercentDifferenceFromLow(position.AverageBuyCostPerShare)),
                MaxGain = Math.Max(context.MaxGain, bar.PercentDifferenceFromHigh(position.AverageBuyCostPerShare)),
                Last10Bars = last10bars
            };
        }

        internal static (decimal, decimal) CalculateMaxDrawdownAndGain(List<PriceBar> last10Bars)
        {
            // using the last 10 bars, the first bar serves as a reference, from that closing price
            // calculate max drawdown and max gain pct seen in those 10 bars
            var maxDrawdownPctRecent = 0m;
            var maxGainPctRecent = 0m;
            var referenceBar = last10Bars.First();
            foreach (var bar in last10Bars)
            {
                maxDrawdownPctRecent = Math.Min(maxDrawdownPctRecent, bar.PercentDifferenceFromLow(referenceBar.Close));
                maxGainPctRecent = Math.Max(maxGainPctRecent, bar.PercentDifferenceFromHigh(referenceBar.Close));
            }

            return (maxDrawdownPctRecent, maxGainPctRecent);
        }

        protected abstract void ApplyPriceBarToPositionInternal(SimulationContext context, PriceBar bar);
    }
}
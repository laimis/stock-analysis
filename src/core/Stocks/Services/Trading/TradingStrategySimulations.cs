using System;
using System.Linq;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services.Trading
{
    internal record struct SimulationContext(
        string Name,
        PositionInstance Position,
        PriceBar[] Prices,
        int NumberOfProfitPoints,
        Func<int, decimal> GetProfitPointFunc,
        Func<int, PositionInstance, decimal> GetStopPriceFunc,
        bool CloseIfOpenAtTheEnd,
        decimal MaxGain,
        decimal MaxDrawdown,
        decimal NumberOfSharesAtStart,
        int CurrentLevel
    )
    {
        internal decimal GetCurrentProfitPoint() =>
            GetProfitPointFunc(CurrentLevel);

        internal decimal? GetStopPrice() =>
            GetStopPriceFunc(CurrentLevel, Position);
    }

    internal class TradingStrategySimulations
    {
        public static TradingStrategyResult Run(
            string name,
            PositionInstance startPosition,
            PriceBar[] prices,
            int numberOfProfitPoints,
            Func<int, decimal> getProfitPointFunc,
            Func<int, PositionInstance, decimal> getStopPriceFunc,
            Func<SimulationContext, PriceBar, SimulationContext> foldFunction,
            bool closeIfOpenAtTheEnd)
        {
            if (startPosition.StopPrice == null)
            {
                throw new InvalidOperationException("Stop price is not set");
            }

            var context = new SimulationContext(
                Name: name,
                Position: startPosition,
                Prices: prices,
                NumberOfProfitPoints: numberOfProfitPoints,
                GetProfitPointFunc: getProfitPointFunc,
                GetStopPriceFunc: getStopPriceFunc,
                CloseIfOpenAtTheEnd: closeIfOpenAtTheEnd,
                MaxGain: 0m,
                MaxDrawdown: 0m,
                NumberOfSharesAtStart: startPosition.NumberOfShares,
                CurrentLevel: 1
            );

            var finalContext = prices.Aggregate(context, foldFunction);

            if (!finalContext.Position.IsClosed && closeIfOpenAtTheEnd)
            {
                ClosePosition(finalContext, prices[^1]);
            }

            return new TradingStrategyResult(
                maxGainPct: finalContext.MaxGain,
                maxDrawdownPct: finalContext.MaxDrawdown,
                position: finalContext.Position,
                strategyName: finalContext.Name
            );
        }

        public static SimulationContext ApplyBar(SimulationContext context, PriceBar bar)
        {
            if (context.Position.IsClosed)
            {
                return context;
            }

            var level = context.CurrentLevel;
            // check if it's the max gain
            if (bar.High >= context.GetCurrentProfitPoint())
            {
                ExecuteProfitSell(context, bar);

                level++;
            }

            // if stop is reached, sell at the close price
            if (bar.Close <= context.Position.StopPrice.Value)
            {
                ClosePosition(context, bar);
            }

            context.Position.SetPrice(bar.Close);

            return context with
            {
                MaxGain = Math.Max(
                    bar.PercentDifferenceFromHigh(context.Position.AverageBuyCostPerShare),
                    context.MaxGain
                ),
                MaxDrawdown = Math.Min(
                    bar.PercentDifferenceFromLow(context.Position.AverageBuyCostPerShare),
                    context.MaxDrawdown
                ),
                CurrentLevel = level
            };
        }

        private static void ClosePosition(SimulationContext context, PriceBar bar)
        {
            context.Position.Sell(
                context.Position.NumberOfShares,
                bar.Close,
                Guid.NewGuid(),
                bar.Date
            );
        }

        private static void ExecuteProfitSell(SimulationContext context, PriceBar bar)
        {
            var portion = (int)context.NumberOfSharesAtStart / context.NumberOfProfitPoints;
            if (portion == 0)
            {
                portion = 1;
            }

            if (context.Position.NumberOfShares < portion)
            {
                portion = (int)context.Position.NumberOfShares;
            }

            if (context.CurrentLevel == context.NumberOfProfitPoints)
            {
                // sell all the remaining shares
                portion = (int)context.Position.NumberOfShares;
            }

            context.Position.Sell(
                portion, 
                context.GetCurrentProfitPoint(),
                Guid.NewGuid(),
                bar.Date
            );

            if (context.Position.NumberOfShares > 0)
            {
                context.Position.SetStopPrice(
                    context.GetStopPrice(),
                    bar.Date
                );
            }
        }

        public class TradingStrategyExecutorWithDownsideProtection
        {
            private bool _executed;
            private int _downsideProtectionSize;

            public TradingStrategyExecutorWithDownsideProtection(int downsideProtectionSize)
            {
                _executed = false;
                _downsideProtectionSize = downsideProtectionSize;
            }

            public SimulationContext Apply(SimulationContext context, PriceBar bar)
            {
                var newContext = ApplyBar(context, bar);

                if (!_executed && context.Position.RR < -0.5m && context.Position.NumberOfShares > 0)
                {
                    var stocksToSell = (int)(context.Position.NumberOfShares / _downsideProtectionSize);
                    if (stocksToSell > 0)
                    {
                        context.Position.Sell(stocksToSell, bar.Close, Guid.NewGuid(), bar.Date);
                        _executed = true;
                    }
                }

                return newContext;
            }
        }
    }
}
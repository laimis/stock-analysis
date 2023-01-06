using System;
using System.Linq;

namespace core.Stocks.Services.Trading
{
    public static class ProfitLevels
    {
        public static decimal? GetProfitPoint(PositionInstance instance, int level)
        {
            if (instance.FirstStop == null)
            {
                return null;
            }

            var riskPerShare = instance.CompletedPositionCostPerShare - instance.FirstStop.Value;

            return instance.CompletedPositionCostPerShare + riskPerShare * level;
        }

        public static decimal? GetProfitPointForPercentGain(PositionInstance instance, int level, decimal percentGain)
        {
            var singleLevel = instance.CompletedPositionCostPerShare * percentGain;

            return instance.CompletedPositionCostPerShare + singleLevel * level;
        }

        public static decimal[] GetProfitPoints(Func<PositionInstance, int, decimal?> profitPointFunc, PositionInstance position, int levels)
        {
            return Enumerable.Range(1, levels)
                .Select(n => profitPointFunc(position, n))
                .Where(p => p.HasValue)
                .Select(p => p.Value)
                .ToArray();
        }

        public record struct ProfitPoints(string name, decimal[] prices);
    }
}
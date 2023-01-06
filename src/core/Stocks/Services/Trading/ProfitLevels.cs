namespace core.Stocks.Services.Trading
{
    public static class ProfitLevels
    {
        public static decimal? GetPricePointForProfitLevel(PositionInstance instance, int level)
        {
            if (instance.FirstStop == null)
            {
                return null;
            }

            var riskPerShare = instance.CompletedPositionCostPerShare - instance.FirstStop.Value;

            return instance.CompletedPositionCostPerShare + riskPerShare * level;
        }

        public static decimal? GetPricePointForPercentLevels(PositionInstance instance, int level, decimal percentGain)
        {
            var singleLevel = instance.CompletedPositionCostPerShare * percentGain;

            return instance.CompletedPositionCostPerShare + singleLevel * level;
        }        
    }
}
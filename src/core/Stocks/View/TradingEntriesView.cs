namespace core.Stocks.Views
{
    public class TradingEntriesView
    {
        public TradingEntriesView(TradingEntryView[] current, PositionInstance[] past)
        {
            Current = current;
            Past = past;
        }

        public TradingEntryView[] Current { get; }
        public PositionInstance[] Past { get; }
    }
}
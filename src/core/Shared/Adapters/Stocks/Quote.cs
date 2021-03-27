namespace core.Adapters.Stocks
{
    public class Quote
    {
        public double Open { get; set; }
        public double Close { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Volume { get; set; }
        public double Change => (Close - Open) / Open;
        public double Range => (Close - Open) / (High - Open);
    }
}
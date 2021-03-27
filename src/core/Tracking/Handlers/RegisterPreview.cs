using core.Adapters.Stocks;

namespace core.Tracking.Handlers
{
    internal class RegisterPreview
    {
        public RegisterPreview(CompanyProfile c, StockAdvancedStats a, Quote q)
        {
            CompanyName = c.CompanyName;
            IssueType = c.IssueType;
            Description = c.Description;
            Website = c.Website;

            Avg30Volume = a.Avg30Volume.HasValue ? a.Avg30Volume.Value : 0;
            Open = q.Open;
            Close = q.Close;
            High = q.High;
            Low = q.Low;
            Change = q.Change;
            Volume = q.Volume;
            Range = q.Range;
        }

        public string CompanyName { get; set; }
        public string IssueType { get; set; }
        public string Description { get; set; }
        public string Website  { get; set; }

        public double Avg30Volume { get; set; }
        public double Open { get; set; }
        public double Close { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Change { get; set; }
        public double Volume { get; set; }
        public double Range { get; set; }
        public double VolumeIncrease => (Volume - Avg30Volume)/Avg30Volume;
    }
}
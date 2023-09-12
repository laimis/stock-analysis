using System.Collections.Generic;

namespace core.Shared.Adapters.Stocks
{
    public class StockProfile
    {
        public string Symbol { get; set; }
        public string Description { get; set; }
        public string SecurityName { get; set; }
        public string IssueType { get; set; }
        public string Exchange { get; set; }
        public string Cusip { get; set; }
        public Dictionary<string, string> Fundamentals { get; set; }
    }
}
namespace core.Adapters.Stocks
{
    public class SearchResult
    {
        public string Symbol { get; set; }
        public string SecurityName { get; set; }
        public string SecurityType { get; set; }
        public string Region { get; set; }
        public string Exchange { get; set; }
        public bool IsCommonShare => SecurityType == "cs";
    }
}
using System.Collections.Generic;

namespace core.Adapters.Stocks
{
    public class SearchResult
    {
        private static readonly HashSet<string> _supportedTypes = new HashSet<string>{
            "SHARE"
        };

        public string Symbol { get; set; }
        public string SecurityName { get; set; }
        public string SecurityType { get; set; }
        public string Region { get; set; }
        public string Exchange { get; set; }
        public bool IsSupportedType => _supportedTypes.Contains(SecurityType) && Region == "US";
    }
}
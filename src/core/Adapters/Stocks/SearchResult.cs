using System.Collections.Generic;

namespace core.Adapters.Stocks
{
    public class SearchResult
    {
        private static readonly HashSet<string> _supportedTypes = new HashSet<string>{
            "ad",
            "re",
            "cs",
            "et",
            "ps"
        };

        public string Symbol { get; set; }
        public string SecurityName { get; set; }
        public string SecurityType { get; set; }
        public string Region { get; set; }
        public string Exchange { get; set; }
        public bool IsSupportedType => _supportedTypes.Contains(SecurityType);
    }
}
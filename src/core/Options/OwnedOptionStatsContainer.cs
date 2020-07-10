using System.Collections.Generic;
using System.Linq;

namespace core.Options
{
    public class OwnedOptionStatsContainer
    {
        public OwnedOptionStatsContainer(IEnumerable<OwnedOptionSummary> summaries)
        {
            Options = summaries;
            
            Overall = new OwnedOptionStats(summaries);
            Buy = new OwnedOptionStats(summaries.Where(s => s.BoughtOrSold == "Bought"));
            Sell = new OwnedOptionStats(summaries.Where(s => s.BoughtOrSold == "Sold"));
        }

        public IEnumerable<OwnedOptionSummary> Options { get; }
        public OwnedOptionStats Overall { get; }
        public OwnedOptionStats Buy { get; }
        public OwnedOptionStats Sell { get; }
    }
}
using System.Collections.Generic;
using System.Linq;

namespace core.Options
{
    public class OwnedOptionStatsContainer
    {
        public OwnedOptionStatsContainer(IEnumerable<OwnedOptionSummary> closed, IEnumerable<OwnedOptionSummary> open)
        {
            ClosedOptions = closed;
            OpenOptions = open;
            
            Overall = new OwnedOptionStats(closed);
            Buy = new OwnedOptionStats(closed.Where(s => s.BoughtOrSold == "Bought"));
            Sell = new OwnedOptionStats(closed.Where(s => s.BoughtOrSold == "Sold"));

        }

        public IEnumerable<OwnedOptionSummary> ClosedOptions { get; }
        public IEnumerable<OwnedOptionSummary> OpenOptions { get; }
        public OwnedOptionStats Overall { get; }
        public OwnedOptionStats Buy { get; }
        public OwnedOptionStats Sell { get; }
    }
}
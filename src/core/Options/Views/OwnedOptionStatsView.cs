using System.Collections.Generic;
using System.Linq;

namespace core.Options
{
    public class OwnedOptionStatsView
    {
        public OwnedOptionStatsView(){}
        public OwnedOptionStatsView(IEnumerable<OwnedOptionView> closed, IEnumerable<OwnedOptionView> open)
        {
            ClosedOptions = closed;
            OpenOptions = open;
            
            Overall = new OwnedOptionStats(closed);
            Buy = new OwnedOptionStats(closed.Where(s => s.BoughtOrSold == "Bought"));
            Sell = new OwnedOptionStats(closed.Where(s => s.BoughtOrSold == "Sold"));

        }

        public IEnumerable<OwnedOptionView> ClosedOptions { get; set; }
        public IEnumerable<OwnedOptionView> OpenOptions { get; set; }
        public OwnedOptionStats Overall { get; set; }
        public OwnedOptionStats Buy { get; set; }
        public OwnedOptionStats Sell { get; set; }
    }
}
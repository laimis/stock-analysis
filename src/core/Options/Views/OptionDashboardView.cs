using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared;

namespace core.Options
{
    public class OptionDashboardView : IViewModel
    {
        public OptionDashboardView(){}
        public OptionDashboardView(IEnumerable<OwnedOptionView> closed, IEnumerable<OwnedOptionView> open)
        {
            ClosedOptions = closed;
            OpenOptions = open;
            
            Overall = new OwnedOptionStats(closed);
            Buy = new OwnedOptionStats(closed.Where(s => s.BoughtOrSold == "Bought"));
            Sell = new OwnedOptionStats(closed.Where(s => s.BoughtOrSold == "Sold"));

            Calculated = DateTimeOffset.UtcNow;
        }

        public IEnumerable<OwnedOptionView> ClosedOptions { get; set; }
        public IEnumerable<OwnedOptionView> OpenOptions { get; set; }
        public OwnedOptionStats Overall { get; set; }
        public OwnedOptionStats Buy { get; set; }
        public OwnedOptionStats Sell { get; set; }
        public DateTimeOffset Calculated { get; set; }
    }
}
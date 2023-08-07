using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared;
using core.Shared.Adapters.Brokerage;

namespace core.Options
{
    public class OptionDashboardView : IViewModel
    {
        public const string Version = "1";
        public OptionDashboardView(){}
        public OptionDashboardView(IEnumerable<OwnedOptionView> closed, IEnumerable<OwnedOptionView> open, IEnumerable<OptionPosition> brokeragePositions)
        {
            Closed = closed;
            Open = open;
            BrokeragePositions = brokeragePositions;
            
            OverallStats = new OwnedOptionStats(closed);
            BuyStats = new OwnedOptionStats(closed.Where(s => s.BoughtOrSold == "Bought"));
            SellStats = new OwnedOptionStats(closed.Where(s => s.BoughtOrSold == "Sold"));

            Calculated = DateTimeOffset.UtcNow;
        }

        public IEnumerable<OwnedOptionView> Closed { get; set; }
        public IEnumerable<OwnedOptionView> Open { get; set; }
        public IEnumerable<OptionPosition> BrokeragePositions { get; set; }
        public OwnedOptionStats OverallStats { get; set; }
        public OwnedOptionStats BuyStats { get; set; }
        public OwnedOptionStats SellStats { get; set; }
        public DateTimeOffset Calculated { get; set; }
    }
}
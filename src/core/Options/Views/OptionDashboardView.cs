using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared;
using core.Shared.Adapters.Brokerage;

namespace core.Options
{
    public class OptionDashboardView
    {
        public const string Version = "1";
        public OptionDashboardView(){}
        public OptionDashboardView(
            IEnumerable<OwnedOptionView> closed,
            IEnumerable<OwnedOptionView> open,
            IEnumerable<OptionPosition> brokeragePositions)
        {
            Closed = closed;
            Open = open;
            BrokeragePositions = brokeragePositions;
            
            OverallStats = new OwnedOptionStats(closed);
            BuyStats = new OwnedOptionStats(closed.Where(s => s.BoughtOrSold == "Bought"));
            SellStats = new OwnedOptionStats(closed.Where(s => s.BoughtOrSold == "Sold"));
        }

        public IEnumerable<OwnedOptionView> Closed { get; }
        public IEnumerable<OwnedOptionView> Open { get; }
        public IEnumerable<OptionPosition> BrokeragePositions { get; }
        public OwnedOptionStats OverallStats { get; }
        public OwnedOptionStats BuyStats { get; }
        public OwnedOptionStats SellStats { get; }
    }
}
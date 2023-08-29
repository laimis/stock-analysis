using System.Collections.Generic;
using System.Linq;
using core.Shared.Adapters.Brokerage;

namespace core.Options
{
    public class OptionDashboardView
    {
        public OptionDashboardView(
            IEnumerable<OwnedOptionView> closed,
            IEnumerable<OwnedOptionView> open,
            IEnumerable<OptionPosition> brokeragePositions,
            IEnumerable<Order> orders)
        {
            Closed = closed;
            Open = open;
            Orders = orders;
            BrokeragePositions = brokeragePositions;
            
            OverallStats = new OwnedOptionStats(closed);
            BuyStats = new OwnedOptionStats(closed.Where(s => s.BoughtOrSold == "Bought"));
            SellStats = new OwnedOptionStats(closed.Where(s => s.BoughtOrSold == "Sold"));
        }

        public IEnumerable<OwnedOptionView> Closed { get; }
        public IEnumerable<OwnedOptionView> Open { get; }
        public IEnumerable<Order> Orders { get; }
        public IEnumerable<OptionPosition> BrokeragePositions { get; }
        public OwnedOptionStats OverallStats { get; }
        public OwnedOptionStats BuyStats { get; }
        public OwnedOptionStats SellStats { get; }
    }
}
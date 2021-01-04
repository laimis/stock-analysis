using System.Collections.Generic;
using System.Linq;

namespace core.Options
{
    public class OwnedOptionStatsView
    {
        public OwnedOptionStatsView(){}
        public OwnedOptionStatsView(IEnumerable<OwnedOptionView> open)
        {
            OpenOptions = open;
        }

        public IEnumerable<OwnedOptionView> OpenOptions { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace core.Stocks
{
    public static class StockCategory
    {
        public static string ShortTerm = "shortterm";
        public static string LongTerm = "longterm";
        public static string Recommendation = "recommendation";

        public static IEnumerable<string> All = new[] { ShortTerm, LongTerm, Recommendation };

        public static string Default => ShortTerm;

        internal static bool IsValid(string category)
        {
            return All.Contains(category);
        }
    }
}
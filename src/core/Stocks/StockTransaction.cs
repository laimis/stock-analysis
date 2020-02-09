using System;
using System.ComponentModel.DataAnnotations;
using core.Shared;

namespace core.Stocks
{
    public class StockTransaction : RequestWithTicker<CommandResponse>
    {
        [Range(1, 10000)]
        public int NumberOfShares { get; set; }

        [Range(1, 10000)]
        public double Price { get; set; }
        
        [Required]
        public DateTimeOffset? Date { get; set; }
    }
}
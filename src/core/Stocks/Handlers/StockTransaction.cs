using System;
using System.ComponentModel.DataAnnotations;
using core.Shared;

namespace core.Stocks
{
    public class StockTransaction : RequestWithTicker<CommandResponse>
    {
        [Range(1, 1000000)]
        public decimal NumberOfShares { get; set; }

        [Range(0, 100000)]
        public decimal Price { get; set; }
        
        [Required]
        public DateTimeOffset? Date { get; set; }

        public string Notes { get; set; }
    }
}
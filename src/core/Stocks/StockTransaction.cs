using System;
using System.ComponentModel.DataAnnotations;
using core.Shared;

namespace core.Stocks
{
    public class StockTransaction : RequestWithUserId
    {
        [Required]
        public string Ticker { get; set; }
        
        [Range(1, 10000)]
        public int Amount { get; set; }

        [Range(1, 10000)]
        public double Price { get; set; }
        
        [Required]
        public DateTimeOffset? Date { get; set; }
    }
}
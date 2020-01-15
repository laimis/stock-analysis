using System;
using System.ComponentModel.DataAnnotations;

namespace core.Stocks
{
    public class StockTransaction
    {
        [Required]
        public string Ticker { get; set; }
        
        [Range(1, double.MaxValue)]
        public int Amount { get; set; }

        [Range(1, 10000)]
        public double Price { get; set; }
        
        [Required]
        public DateTime? Date { get; set; }
    }
}
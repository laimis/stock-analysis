using System;
using System.ComponentModel.DataAnnotations;
using core.Utils;

namespace core.Options
{
    public class SellOption
    {
        [Required]
        public string Ticker { get; set; }
        
        [Range(1, double.MaxValue)]
        public double StrikePrice { get; set; }
        
        [Required]
        [NotInThePast]
        public DateTimeOffset? ExpirationDate { get; set; }
        
        [Required]
        public string OptionType { get; set; }
        
        [Range(1, 1000, ErrorMessage = "Invalid number of contracts specified")]
        public int Amount { get; set; }

        [Range(1, 1000)]
        public double Premium { get; set; }

        [Required]
        public DateTimeOffset? Filled { get; set; }
    }
}
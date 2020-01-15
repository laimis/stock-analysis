using System;
using System.ComponentModel.DataAnnotations;
using core.Utils;

namespace core.Options
{
    public class CloseOption
    {
        [Required]
        public string Ticker { get; set; }
        
        [Range(1, 10000)]
        public double StrikePrice { get; set; }
        
        [Required]
        public DateTimeOffset? Expiration { get; set; }
        
        [Required]
        public string OptionType { get; set; }
        
        [Range(1, 1000, ErrorMessage = "Invalid number of contracts specified")]
        public int Amount { get; set; }

        [Range(0, 1000)]
        public double? ClosePrice { get; set; }

        [Required]
        public DateTimeOffset? CloseDate { get; set; }
    }
}
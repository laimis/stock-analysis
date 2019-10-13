using System;
using System.ComponentModel.DataAnnotations;
using web.Utils;

namespace web
{
    public class OpenModel
    {
        [Required]
        public string Ticker { get; set; }
        
        [Range(1, float.MaxValue)]
        public float StrikePrice { get; set; }
        
        [Required]
        [NotInThePast]
        public DateTimeOffset? ExpirationDate { get; set; }
        
        [Required]
        public string OptionType { get; set; }
        
        [Range(1, 1000)]
        public int Amount { get; set; }

        [Range(1, float.MaxValue)]
        public float Bid { get; set; }

        [Required]
        public DateTimeOffset? Filled { get; set; }
    }
}
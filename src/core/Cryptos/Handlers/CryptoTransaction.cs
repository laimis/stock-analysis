using System;
using System.ComponentModel.DataAnnotations;
using core.Shared;

namespace core.Cryptos.Handlers
{
    public class CryptoTransaction : RequestWithToken<CommandResponse>
    {
        [Range(0.00000000000000000001, 1000000)]
        public decimal Quantity { get; set; }

        [Range(0, 100000)]
        public decimal DollarAmount { get; set; }
        
        [Required]
        public DateTimeOffset? Date { get; set; }

        public string Notes { get; set; }
    }
}
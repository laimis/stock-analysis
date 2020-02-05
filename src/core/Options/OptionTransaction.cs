using System;
using System.ComponentModel.DataAnnotations;
using core.Shared;
using core.Utils;

namespace core.Options
{
    public class OptionTransaction : RequestWithUserId<CommandResponse<OwnedOption>>
    {
        private Ticker _ticker;
        [Required]
        public string Ticker 
        {
            get { return _ticker;}
            set { _ticker = new Ticker(value); }
        }

        [Range(1, 10000)]
        public double StrikePrice { get; set; }

        [Required]
        public DateTimeOffset? ExpirationDate { get; set; }

        [Required]
        [ValidValues(nameof(core.Options.OptionType.CALL), nameof(core.Options.OptionType.PUT))]
        public string OptionType { get; set; }

        [Range(1, 1000, ErrorMessage = "Invalid number of contracts specified")]
        public int NumberOfContracts { get; set; }

        [Range(1, 1000)]
        public double Premium { get; set; }

        [Required]
        public DateTimeOffset? Filled { get; set; }
    }
}
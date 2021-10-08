using System;
using System.ComponentModel.DataAnnotations;
using core.Shared;
using core.Utils;

namespace core.Options
{
    public class OptionTransaction : RequestWithTicker<CommandResponse<OwnedOption>>
    {
        [Range(1, 10000)]
        public decimal StrikePrice { get; set; }

        [Required]
        public DateTimeOffset? ExpirationDate { get; set; }

        [Required]
        [ValidValues(nameof(core.Options.OptionType.CALL), nameof(core.Options.OptionType.PUT))]
        public string OptionType { get; set; }

        [Range(1, 10000, ErrorMessage = "Invalid number of contracts specified")]
        public int NumberOfContracts { get; set; }

        [Range(1, 100000)]
        public decimal Premium { get; set; }

        [Required]
        public DateTimeOffset? Filled { get; set; }

        public string Notes { get; set; }
    }
}
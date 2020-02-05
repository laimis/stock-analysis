using System;
using System.ComponentModel.DataAnnotations;
using core.Shared;

namespace core.Stocks
{
    public class StockTransaction : RequestWithUserId<CommandResponse>
    {
        private Ticker _ticker;
        [Required]
        public string Ticker 
        {
            get { return _ticker;}
            set { _ticker = new Ticker(value); }
        }
        
        [Range(1, 10000)]
        public int NumberOfShares { get; set; }

        [Range(1, 10000)]
        public double Price { get; set; }
        
        [Required]
        public DateTimeOffset? Date { get; set; }
    }
}
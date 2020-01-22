using System;
using System.ComponentModel.DataAnnotations;
using core.Shared;
using MediatR;

namespace core.Stocks
{
    public class StockTransaction : IRequest
    {
        public class Command : RequestWithUserId
        {
            [Required]
            public string Ticker { get; set; }
            
            [Range(1, 10000)]
            public int Amount { get; set; }

            [Range(1, 10000)]
            public double Price { get; set; }
            
            [Required]
            public DateTime? Date { get; set; }
        }
    }
}
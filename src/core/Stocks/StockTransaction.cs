using System;
using System.ComponentModel.DataAnnotations;
using MediatR;

namespace core.Stocks
{
    public class StockTransaction : IRequest
    {
        public class Command : IRequest
        {
            [Required]
            public string Ticker { get; set; }
            
            [Range(1, double.MaxValue)]
            public int Amount { get; set; }

            [Range(1, 10000)]
            public double Price { get; set; }
            
            [Required]
            public DateTime? Date { get; set; }

            public string UserId { get; private set; }
            public void WithUser(string userId)
            {
                this.UserId = userId;
            }
        }
    }
}
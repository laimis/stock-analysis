using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Portfolio.Output;
using core.Shared;

namespace core.Options
{
    public class List
    {
        public class Query : RequestWithUserId<object>
        {
            public Query(string ticker, bool? activeFilter, Guid userId) :base(userId)
            {
                this.Ticker = ticker;
                this.ActiveFilter = activeFilter;
            }

            [Required]
            public string Ticker { get; set; }
            public bool? ActiveFilter { get; }
        }

        public class Handler : HandlerWithStorage<Query, object>
        {
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                var options = await _storage.GetOwnedOptions(request.UserId);

                options = options.Where(o => !o.State.Deleted);

                if (request.ActiveFilter.HasValue)
                {
                    options = options.Where(o => o.IsActive == request.ActiveFilter.Value);
                }

                if (request.Ticker != null)
                {
                    options = options.Where(o => o.Ticker == request.Ticker);
                }

                options = options.OrderByDescending(o => o.State.FirstFill);

                return Map(options);
            }

            private object Map(IEnumerable<OwnedOption> options)
            {
                var summaries = options.Select(o => Map(o));

                var overall = new OwnedOptionStats(summaries);
                var buy = new OwnedOptionStats(summaries.Where(s => s.BoughtOrSold == "Bought"));
                var sell = new OwnedOptionStats(summaries.Where(s => s.BoughtOrSold == "Sold"));

                return new {
                    overall,
                    buy,
                    sell,
                    options = summaries
                };
            }

            private OwnedOptionSummary Map(OwnedOption o)
            {
                return new OwnedOptionSummary
                {
                    Id = o.State.Id,
                    Ticker = o.State.Ticker,
                    OptionType = o.State.OptionType.ToString(),
                    StrikePrice = o.State.StrikePrice,
                    PremiumReceived = o.State.PremiumReceived,
                    PremiumPaid = o.State.PremiumPaid,
                    ExpirationDate = o.State.Expiration.ToString("yyyy-MM-dd"),
                    NumberOfContracts = Math.Abs(o.State.NumberOfContracts),
                    BoughtOrSold = o.State.SoldToOpen.Value ? "Sold" : "Bought",
                    Filled = o.State.FirstFill,
                    Days = o.State.Days,
                    DaysHeld = o.State.DaysHeld,
                    Transactions = new TransactionList(o.State.Transactions.Where(t => !t.IsPL), null, null),
                    ExpiresSoon = o.ExpiresSoon,
                    IsExpired = o.IsExpired,
                    Assigned = o.State.Assigned,
                };
            }
        }
    }

    public class OwnedOptionSummary
    {
        public Guid Id { get; set; }
        public string Ticker { get; set; }
        public string OptionType { get; set; }
        public double StrikePrice { get; set; }
        public double PremiumReceived { get; set; }
        public double PremiumPaid { get; set; }
        public double PremiumCapture
        {
            get
            {
                if (this.BoughtOrSold == "Bought")
                {
                    return (PremiumReceived - PremiumPaid) / PremiumPaid;
                }

                return (PremiumReceived - PremiumPaid) / PremiumReceived;
            }
        }
        public double Profit => this.PremiumReceived - this.PremiumPaid;
        public string ExpirationDate { get; set; }
        public int NumberOfContracts { get; set; }
        public string BoughtOrSold { get; set; }
        public DateTimeOffset Filled { get; set; }
        public double Days { get; set; }
        public int DaysHeld { get; set; }
        public TransactionList Transactions { get; set; }
        public bool ExpiresSoon { get; set; }
        public bool IsExpired { get; set; }
        public bool Assigned { get; set; }
    }
}
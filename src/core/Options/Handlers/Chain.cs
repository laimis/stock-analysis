﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Options;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using MediatR;

namespace core.Options
{
    public class Chain
    {
        public class Query : RequestWithUserId<OptionDetailsViewModel>
        {
            public Query(string ticker, Guid userId) : base(userId)
            {
                Ticker = ticker;
            }

            public string Ticker { get; }
        }

        public class Handler : IRequestHandler<Query, OptionDetailsViewModel>
        {
            private IAccountStorage _accounts;
            private IBrokerage _brokerage;
            
            public Handler(IAccountStorage accounts, IBrokerage brokerage)
            {
                _accounts = accounts;
                _brokerage = brokerage;
            }
            
            public async Task<OptionDetailsViewModel> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(request.UserId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                var priceResult = await _brokerage.GetQuote(user.State, request.Ticker);
                var price = priceResult.IsOk switch {
                    true => priceResult.Success.Price,
                    false => (decimal?)null
                };

                var detailsResponse = await _brokerage.GetOptions(user.State, request.Ticker);
                if (!detailsResponse.IsOk)
                {
                    throw new InvalidOperationException("Failed to get options: " + detailsResponse.Error.Message);
                }

                return MapOptionDetails(price, detailsResponse.Success!);
            }
        }

        public static OptionDetailsViewModel MapOptionDetails(
            decimal? price,
            OptionChain chain)
        {
            var optionList = chain.Options;

            var puts = optionList.Where(o => o.IsPut);
            var calls = optionList.Where(o => o.IsCall);

            var callAverageVolume = calls.Average(o => o.Volume);
            var priceBasedOnCalls = callAverageVolume switch {
                0 => 0,
                _ => calls.Average(o => o.Volume * o.StrikePrice) / (decimal)callAverageVolume
            };

            var putAverageVolume = puts.Average(o => o.Volume);
            var priceBasedOnPuts = putAverageVolume switch {
                0 => 0,
                _ => puts.Average(o => o.Volume * o.StrikePrice) / (decimal)putAverageVolume
            };

            return new OptionDetailsViewModel
            {
                StockPrice = price,
                Options = chain.Options,
                Expirations = chain.Options.Select(o => o.ExpirationDate).Distinct().ToArray(),
                Volatility = chain.Volatility,
                NumberOfContracts = chain.NumberOfContracts,
            };
        }
    }
}
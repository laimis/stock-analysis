﻿using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using core.Account;
using core.fs;
using core.fs.Adapters.Brokerage;
using core.fs.Adapters.Stocks;
using core.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Core;
using Nito.AsyncEx;
using Polly;
using Polly.RateLimit;
using Polly.Retry;
using Polly.Timeout;
using storage.shared;

namespace tdameritradeclient;
public class TDAmeritradeClient : IBrokerage
{
    private readonly ILogger<TDAmeritradeClient>? _logger;
    private readonly string _callbackUrl;
    private readonly string _clientId;

    private const string ApiUrl = "https://api.tdameritrade.com/v1";
    private const string AuthUrl = "https://auth.tdameritrade.com";

    private readonly HttpClient _httpClient;
    
    private static readonly AsyncRetryPolicy _retryPolicy = Policy
        .HandleInner<RateLimitRejectedException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
        );

    private static readonly AsyncRateLimitPolicy _rateLimit = Policy.RateLimitAsync(
        numberOfExecutions: 20,
        perTimeSpan: TimeSpan.FromSeconds(1),
        maxBurst: 10);

    private static readonly AsyncTimeoutPolicy _timeoutPolicy = Policy.TimeoutAsync(
        seconds: (int)TimeSpan.FromSeconds(15).TotalSeconds
    );

    private readonly AsyncPolicy _wrappedPolicy = Policy.WrapAsync(
        _rateLimit, _timeoutPolicy, _retryPolicy
    );
    

    private readonly IBlobStorage _blogStorage;

    public TDAmeritradeClient(IBlobStorage blobStorage, string callbackUrl, string clientId, ILogger<TDAmeritradeClient>? logger)
    {
        _blogStorage = blobStorage;
        _callbackUrl = callbackUrl;
        _clientId = clientId;
        _logger = logger;
        _httpClient = new HttpClient();
    }
    
    private static FSharpResult<T,ServiceError> ToFSharpError<T>(ServiceError error) =>
        FSharpResult<T,ServiceError>.NewError(error);

    private static FSharpResult<T, ServiceError> ToFSharpError<T>(string message) =>
        ToFSharpError<T>(new ServiceError(message));
    
    private static FSharpResult<T, ServiceError> ToFSharpResult<T>(T value) =>
        FSharpResult<T, ServiceError>.NewOk(value);

    public async Task<OAuthResponse?> ConnectCallback(string code)
    {
        var postData = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", code },
            { "access_type", "offline" },
            { "redirect_uri", _callbackUrl },
            { "client_id", _clientId }
        };

        var content = new FormUrlEncodedContent(postData);

        var response = await _httpClient.PostAsync(GenerateApiUrl("/oauth2/token"), content);

        LogIfFailed(response, "connect callback");

        var responseString = await response.Content.ReadAsStringAsync();

        _logger?.LogDebug("Response from tdameritrade: {ameritraderesponse}", responseString);

        return JsonSerializer.Deserialize<OAuthResponse>(responseString);
    }

    private async void LogIfFailed(HttpResponseMessage response, string message)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            _logger?.LogError("TDAmeritrade client failed with {statusCode} for {message}: {content}", response.StatusCode, message, content);
        }
    }

    public Task<string> GetOAuthUrl()
    {
        var encodedClientId = Uri.EscapeDataString($"{_clientId}@AMER.OAUTHAP");
        var encodedCallbackUrl = Uri.EscapeDataString(_callbackUrl);
        var url = $"{AuthUrl}/auth?response_type=code&redirect_uri={encodedCallbackUrl}&client_id={encodedClientId}";
        
        return Task.FromResult(url);
    }

    public async Task<FSharpResult<StockProfile,ServiceError>> GetStockProfile(UserState state, Ticker ticker)
    {
        if (state.ConnectedToBrokerage == false)
        {
            return NotConnectedToBrokerageError<StockProfile>();
        }
        
        var function = $"instruments?symbol={ticker.Value}&projection=fundamental";

        var results = await CallApi<Dictionary<string, SearchItemWithFundamental>>(
            state, function, HttpMethod.Get
        );

        if (results.IsError)
        {
            return ToFSharpError<StockProfile>(results.ErrorValue);
        }

        var fundamentals = results.ResultValue[ticker.Value].fundamental ?? new Dictionary<string, object>();
        var data = results.ResultValue[ticker.Value];

        var mapped = new StockProfile {
            Symbol = data.symbol,
            Description = data.description,
            SecurityName = data.description,
            Exchange = data.exchange,
            Cusip = data.cusip,
            IssueType = data.assetType,
            Fundamentals = fundamentals.ToDictionary(
                f => f.Key,
                f => f.Value.ToString()
            )
        };

        return ToFSharpResult(mapped);
    }

    public async Task<FSharpResult<SearchResult[], ServiceError>> Search(UserState state, string query, int limit)
    {
        if (state.ConnectedToBrokerage == false)
        {
            return NotConnectedToBrokerageError<SearchResult[]>();
        }
        
        var function = $"instruments?symbol={query}.*&projection=symbol-regex";

        var results = await CallApi<Dictionary<string, SearchItem>>(state, function, HttpMethod.Get);
        if (results.IsOk == false)
        {
            return ToFSharpError<SearchResult[]>(results.ErrorValue);
        }

        var converted = results.ResultValue.Values.Select(
            i => new SearchResult
            {
                Symbol = i.symbol,
                SecurityName = i.description,
                Exchange = i.exchange,
                SecurityType = i.assetType
            }
            ).OrderBy(r =>
                r.Exchange switch {
                    "Pink Sheet" => 1,
                    _ => 0
                }
            )
            .ThenBy(r =>
                r.SecurityType switch {
                    "EQUITY" => 0,
                    "OPTION" => 1,
                    "MUTUAL_FUND" => 2,
                    "ETF" => 3,
                    "INDEX" => 4,
                    "CASH_EQUIVALENT" => 5,
                    "FIXED_INCOME" => 6,
                    "CURRENCY" => 7,
                    _ => 8
                }
            )
            .Take(limit)
            .ToArray();

        return ToFSharpResult(converted);
    }

    public async Task<FSharpResult<BrokerageAccount,ServiceError>> GetAccount(UserState user)
    {
        if (user.ConnectedToBrokerage == false)
        {
            return NotConnectedToBrokerageError<BrokerageAccount>();
        }
        
        var response = await CallApi<AccountsResponse[]>(
            user,
            "/accounts?fields=positions,orders",
            HttpMethod.Get
        );

        if (response.IsError)
        {
            return ToFSharpError<BrokerageAccount>(response.ErrorValue);
        }

        var accounts = response.ResultValue;

        var tdPositions = (accounts[0].securitiesAccount?.positions)
            ?? throw new Exception("Could not find positions in response");
        var strategies = (accounts[0].securitiesAccount?.orderStrategies)
            ?? throw new Exception("Could not find order strategies in response");

        var orders = 
            strategies
            .SelectMany(o => (o.orderLegCollection ?? Array.Empty<OrderLeg>()).Select(l => (o, l)))
            .Select( tuple => new Order
        {
            Date = tuple.o.closeTime == null ? null : DateTimeOffset.Parse(tuple.o.closeTime),
            Status = tuple.o.status,
            OrderId = tuple.o.orderId.ToString(),
            Cancelable = tuple.o.cancelable,
            Price = tuple.o.ResolvePrice(),
            Quantity = Convert.ToInt32(tuple.o.quantity),
            Ticker = new FSharpOption<Ticker>(new Ticker(tuple.l.instrument?.resolvedSymbol)),
            Description = tuple.l.instrument?.description,
            Type = tuple.l.instruction,
            AssetType = tuple.l.instrument?.assetType
        })
        .ToArray();

        var stockPositions = tdPositions
            .Where(p => p.instrument?.assetType == "EQUITY")
            .Select(p => new StockPosition(
                new Ticker(p.instrument?.resolvedSymbol),
                p.averagePrice,
                p.longQuantity > 0 ? p.longQuantity : p.shortQuantity * -1))
            .ToArray();

        var optionPositions = tdPositions
            .Where(p => p.instrument?.assetType == "OPTION")
            .Select(p => {
                var description = p.instrument?.description;
                // description looks like this: AGI Jul 21 2023 13.0 Call
                // AGI is ticker, Jul 21 2023 is expiration date, 13.0 is strike price
                // and Call is CALL type, parse all of these values from the description

                var parts = description?.Split(" ");
                if (parts == null || parts.Length != 6)
                {
                    throw new Exception("Could not parse option description: " + description);
                }

                var ticker = parts[0];
                var expiration = parts[1] + " " + parts[2] + " " + parts[3];
                var strike = Convert.ToDecimal(parts[4]);
                var type = parts[5];

                return new OptionPosition
                {
                    Ticker = new FSharpOption<Ticker>(new Ticker(ticker)),
                    Quantity = p.longQuantity > 0 ? p.longQuantity : p.shortQuantity * -1,
                    AverageCost = p.averagePrice,
                    StrikePrice = strike,
                    ExpirationDate = expiration,
                    MarketValue = p.marketValue,
                    OptionType = type.ToUpperInvariant()
                };
            }).ToArray();

        var account = new BrokerageAccount
        {
            Orders = orders,
            StockPositions = stockPositions,
            OptionPositions = optionPositions,
            CashBalance = accounts[0].securitiesAccount?.currentBalances?.cashBalance,
            Equity = accounts[0].securitiesAccount?.currentBalances?.equity,
            LongMarketValue = accounts[0].securitiesAccount?.currentBalances?.longMarketValue,
            ShortMarketValue = accounts[0].securitiesAccount?.currentBalances?.shortMarketValue,
        };

        return ToFSharpResult(account);
    }

    public async Task<FSharpResult<Unit,ServiceError>> CancelOrder(UserState user, string orderId)
    {
        if (user.ConnectedToBrokerage == false)
        {
            return NotConnectedToBrokerageError<Unit>();
        }
        
        // get account first
        var response = await CallApi<AccountsResponse[]>(user, "/accounts", HttpMethod.Get);

        if (response.IsError)
        {
            return ToFSharpError<Unit>(response.ErrorValue);
        }

        var accounts = response.ResultValue;

        var accountId = accounts[0].securitiesAccount?.accountId;

        var url = $"/accounts/{accountId}/orders/{orderId}";

        var (cancelResponse, content) = await CallApiWithoutSerialization(user, url, HttpMethod.Delete);
        return cancelResponse.IsSuccessStatusCode switch
        {
            true => ToFSharpResult<Unit>(null!),
            false => ToFSharpError<Unit>(content)
        };
    }

    public Task<FSharpResult<Unit,ServiceError>> BuyOrder(
        UserState user,
        Ticker ticker,
        decimal numberOfShares,
        decimal price,
        BrokerageOrderType type,
        BrokerageOrderDuration duration)
    {
        var legCollection = new {
            instruction = "Buy",
            quantity = numberOfShares,
            instrument = new {
                symbol = ticker.Value,
                assetType = "EQUITY"
            }
        };

        var postData = new
        {
            orderType = GetBuyOrderType(type),
            session = GetSession(duration),
            duration = GetBuyOrderDuration(duration),
            price = GetPrice(type, price),
            stopPrice = GetActivationPrice(type, price),
            orderStrategyType = "SINGLE",
            orderLegCollection = new [] {legCollection}
        };

        return EnterOrder(user, postData);
    }
    
    public Task<FSharpResult<Unit,ServiceError>> BuyToCoverOrder(
        UserState user,
        Ticker ticker,
        decimal numberOfShares,
        decimal price,
        BrokerageOrderType type,
        BrokerageOrderDuration duration)
    {
        var legCollection = new {
            instruction = "BUY_TO_COVER",
            quantity = numberOfShares,
            instrument = new {
                symbol = ticker.Value,
                assetType = "EQUITY"
            }
        };

        var postData = new
        {
            orderType = GetBuyOrderType(type),
            session = GetSession(duration),
            duration = GetBuyOrderDuration(duration),
            price = GetPrice(type, price),
            stopPrice = GetActivationPrice(type, price),
            orderStrategyType = "SINGLE",
            orderLegCollection = new [] {legCollection}
        };

        return EnterOrder(user, postData);
    }
    
    public Task<FSharpResult<Unit,ServiceError>> SellShortOrder(
        UserState user,
        Ticker ticker,
        decimal numberOfShares,
        decimal price,
        BrokerageOrderType type,
        BrokerageOrderDuration duration)
    {
        var legCollection = new {
            instruction = "SELL_SHORT",
            quantity = numberOfShares,
            instrument = new {
                symbol = ticker.Value,
                assetType = "EQUITY"
            }
        };

        var postData = new
        {
            orderType = GetBuyOrderType(type),
            session = GetSession(duration),
            duration = GetBuyOrderDuration(duration),
            price = GetPrice(type, price),
            stopPrice = GetActivationPrice(type, price),
            orderStrategyType = "SINGLE",
            orderLegCollection = new [] {legCollection}
        };

        return EnterOrder(user, postData);
    }

    public Task<FSharpResult<Unit,ServiceError>> SellOrder(
        UserState user,
        Ticker ticker,
        decimal numberOfShares,
        decimal price,
        BrokerageOrderType type,
        BrokerageOrderDuration duration)
    {
        var legCollection = new {
            instruction = "Sell",
            quantity = numberOfShares,
            instrument = new {
                symbol = ticker.Value,
                assetType = "EQUITY"
            }
        };

        var postData = new
        {
            orderType = GetBuyOrderType(type),
            session = GetSession(duration),
            duration = GetBuyOrderDuration(duration),
            price = GetPrice(type, price),
            orderStrategyType = "SINGLE",
            orderLegCollection = new [] {legCollection}
        };

        return EnterOrder(user, postData);
    }

    public async Task<FSharpResult<StockQuote,ServiceError>> GetQuote(UserState user, Ticker ticker)
    {
        if (user.ConnectedToBrokerage == false)
        {
            return NotConnectedToBrokerageError<StockQuote>();
        }
        
        var function = $"marketdata/{ticker.Value}/quotes";

        var response = await CallApi<Dictionary<string, StockQuote>>(user, function, HttpMethod.Get);
        if (response.IsError)
        {
            return ToFSharpError<StockQuote>(response.ErrorValue);
        }

        if (!response.ResultValue.TryGetValue(ticker.Value, out var quote))
        {
            return ToFSharpError<StockQuote>("Could not find quote for ticker");
        }

        return ToFSharpResult(quote);
    }

    public async Task<FSharpResult<Dictionary<Ticker, StockQuote>, ServiceError>> GetQuotes(UserState user, IEnumerable<Ticker> tickers)
    {
        if (user.ConnectedToBrokerage == false)
        {
            return NotConnectedToBrokerageError<Dictionary<Ticker, StockQuote>>();
        }
        
        var function = $"marketdata/quotes?symbol={string.Join(",", tickers.Select(t => t.Value))}";

        var result = await CallApi<Dictionary<string, StockQuote>>(user, function, HttpMethod.Get);

        // doing this conversion as I can't seem to find a way for system.text.json to support dictionary deserialization where
        // the key is something other than string. If I put Ticker in there, I get a runtime error. Tried to use converters
        // but that did not work, seems like you had to add dictionary converter and it got ugly pretty quickly
        return result.IsOk switch
        {
            false => ToFSharpError<Dictionary<Ticker, StockQuote>>(result.ErrorValue),
            true => ToFSharpResult(
                result.ResultValue.ToDictionary(keySelector: pair => new Ticker(pair.Key), pair => pair.Value)
            )
        };
    }

    private static FSharpResult<T,ServiceError> NotConnectedToBrokerageError<T>()
    {
        return ToFSharpError<T>("User is not connected to brokerage");
    }

    public async Task<FSharpResult<core.fs.Adapters.Options.OptionChain, ServiceError>> GetOptions(UserState state, Ticker ticker, FSharpOption<DateTimeOffset> expirationDate, FSharpOption<decimal> strikePrice, FSharpOption<string> contractType)
    {
        if (state.ConnectedToBrokerage == false)
        {
            return NotConnectedToBrokerageError<core.fs.Adapters.Options.OptionChain>();
        }
        
        var function = $"marketdata/chains?symbol={ticker.Value}";

        if (FSharpOption<string>.get_IsSome(contractType))
        {
            function += $"&contractType={contractType.Value}";
        }

        if (FSharpOption<DateTimeOffset>.get_IsSome(expirationDate))
        {
            function += $"&fromDate={expirationDate.Value:yyyy-MM-dd}";
            function += $"&toDate={expirationDate.Value:yyyy-MM-dd}";
        }

        if (FSharpOption<decimal>.get_IsSome(strikePrice))
        {
            function += $"&strike={strikePrice.Value}";
        }

        var chainResponse = await CallApi<OptionChain>(state, function, HttpMethod.Get);

        if (chainResponse.IsError)
        {
            return ToFSharpError<core.fs.Adapters.Options.OptionChain>(chainResponse.ErrorValue);
        }

        static IEnumerable<core.fs.Adapters.Options.OptionDetail> ToOptionDetails(Dictionary<string, OptionDescriptorMap> map, decimal? underlyingPrice) =>
            map.SelectMany(kp => kp.Value.Values).SelectMany(v => v)
            .Select(d => new core.fs.Adapters.Options.OptionDetail(symbol: d.symbol!, side: d.putCall?.ToLower()!, description: d.description!) {
                Ask = d.ask,
                Bid = d.bid,
                StrikePrice = d.strikePrice,
                Volume = d.totalVolume,
                OpenInterest = d.openInterest,
                ParsedExpirationDate = d.ExpirationDate,
                DaysToExpiration = d.daysToExpiration,
                Delta = d.delta,
                Gamma = d.gamma,
                Theta = d.theta,
                Vega = d.vega,
                Rho = d.rho,
                ExchangeName = d.exchangeName,
                InTheMoney = d.inTheMoney,
                IntrinsicValue = d.intrinsicValue,
                TimeValue = d.timeValue,
                Volatility = d.volatility,
                MarkChange = d.markChange,
                MarkPercentChange = d.markPercentChange,
                UnderlyingPrice = underlyingPrice
            });

        var chain = chainResponse.ResultValue;

        var response = new core.fs.Adapters.Options.OptionChain(
            symbol: chain.symbol!,
            volatility: chain.volatility,
            numberOfContracts: chain.numberOfContracts,
            options: ToOptionDetails(chain.callExpDateMap!, chain.underlyingPrice).Union(ToOptionDetails(chain.putExpDateMap!, chain.underlyingPrice)).ToArray()
        );

        return FSharpResult<core.fs.Adapters.Options.OptionChain,ServiceError>.NewOk(response);
    }

    public async Task<FSharpResult<MarketHours, ServiceError>> GetMarketHours(UserState state, DateTimeOffset date)
    {
        if (state.ConnectedToBrokerage == false)
        {
            return NotConnectedToBrokerageError<MarketHours>();
        }
        
        var dateStr = date.ToString("yyyy-MM-dd");
        var function = $"marketdata/EQUITY/hours?date={dateStr}";

        var wrapper = await CallApi<MarketHoursWrapper>(state, function, HttpMethod.Get, jsonData: null);
        if (wrapper.IsError)
        {
            return ToFSharpError<MarketHours>(wrapper.ErrorValue);
        }

        if (wrapper.ResultValue.equity == null)
        {
            return ToFSharpError<MarketHours>("Could not find market hours for date");
        }

        if (wrapper.ResultValue.equity.EQ == null)
        {
            return ToFSharpError<MarketHours>("Could not find market hours for date (EQ)");
        }

        return ToFSharpResult(wrapper.ResultValue.equity.EQ);
    }


    public async Task<FSharpResult<PriceBars, ServiceError>> GetPriceHistory(
        UserState state,
        Ticker ticker,
        PriceFrequency frequency,
        FSharpOption<DateTimeOffset> start,
        FSharpOption<DateTimeOffset> end)
    {
        if (state.ConnectedToBrokerage == false)
        {
            return NotConnectedToBrokerageError<PriceBars>();
        }

        long FromOption(FSharpOption<DateTimeOffset> option, DateTimeOffset defaultValue)
        {
            var value = (FSharpOption<DateTimeOffset>.get_IsSome(option)) switch {
                true => option.Value,
                false => defaultValue
            };
            
            return value.ToUnixTimeMilliseconds();
        }

        var startUnix = FromOption(start, DateTimeOffset.UtcNow.AddYears(-2));
        var endUnix = FromOption(end, DateTimeOffset.UtcNow);
        
        var frequencyType = frequency.Tag switch {
            PriceFrequency.Tags.Daily => "daily",
            PriceFrequency.Tags.Weekly => "weekly",
            PriceFrequency.Tags.Monthly => "monthly",
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, null)
        };
        
        var function = $"marketdata/{ticker.Value}/pricehistory?periodType=month&frequencyType={frequencyType}&startDate={startUnix}&endDate={endUnix}";

        var response = await CallApi<PriceHistoryResponse>(
            state,
            function,
            HttpMethod.Get
        );

        if (response.IsError)
        {
            return ToFSharpError<PriceBars>(response.ErrorValue);
        }

        var prices = response.ResultValue;

        if (prices.candles == null)
        {
            throw new Exception($"Null candles for historical prices for {ticker} {start} {end}");
        }

        var payload = prices.candles.Select(c => new PriceBar(
            date: DateTimeOffset.FromUnixTimeMilliseconds(c.datetime),
            c.open,
            high: c.high,
            low: c.low,
            close: c.close,
            volume: c.volume
        )).ToArray();

        return payload.Length == 0
            ? ToFSharpError<PriceBars>(
                $"No candles for historical prices for {ticker.Value}"
            )
            : ToFSharpResult(
                new PriceBars(payload)
            );
    }

    private async Task<FSharpResult<Unit,ServiceError>> EnterOrder(UserState user, object postData)
    {
        var response = await CallApi<AccountsResponse[]>(user, "/accounts", HttpMethod.Get);

        if (response.IsError)
        {
            return ToFSharpError<Unit>(response.ErrorValue);
        }

        var accounts = response.ResultValue;

        var accountId = accounts[0].securitiesAccount?.accountId;

        var url = $"/accounts/{accountId}/orders";

        var data = JsonSerializer.Serialize(postData);

        var (enterResponse, content) = await CallApiWithoutSerialization(user, url, HttpMethod.Post, data);
        
        return enterResponse.IsSuccessStatusCode switch
        {
            true => ToFSharpResult<Unit>(null!),
            false => ToFSharpError<Unit>(content)
        };
    }

    private static string GetBuyOrderType(BrokerageOrderType type)
    {
        if (type.IsLimit)
        {
            return "LIMIT";
        }

        if (type.IsMarket)
        {
            return "MARKET";
        }

        if (type.IsStopMarket)
        {
            return "STOP";
        }

        throw new ArgumentException("Unknown order type: " + type);
    }
        

    private static decimal? GetPrice(BrokerageOrderType type, decimal? price) =>
        (type.IsLimit,type.IsMarket,type.IsStopMarket) switch {
            (true, false, false) => price,
            (false, true, false) => null,
            (false, false, true) => null,
            _ => throw new ArgumentException("Unknown order type: " + type)
        };

    private static decimal? GetActivationPrice(BrokerageOrderType type, decimal? price) =>
        (type.IsLimit,type.IsMarket,type.IsStopMarket) switch {
            (true, false, false) => null,
            (false, true, false) => null,
            (false, false, true) => price,
            _ => throw new ArgumentException("Unknown order type: " + type)
        };

    private static string GetBuyOrderDuration(BrokerageOrderDuration duration) =>
        (duration.IsDay, duration.IsGtc, duration.IsDayPlus, duration.IsGtcPlus) switch {
            (true, false, false, false) => "DAY",
            (false, true, false, false) => "GOOD_TILL_CANCEL",
            (false, false, true, false) => "DAY",
            (false, false, false, true) => "GOOD_TILL_CANCEL",
            _ => throw new ArgumentException("Unknown order type: " + duration)
        };

    private static string GetSession(BrokerageOrderDuration duration) =>
        (duration.IsDay, duration.IsGtc, duration.IsDayPlus, duration.IsGtcPlus) switch {
            (true, false, false, false) => "NORMAL",
            (false, true, false, false) => "NORMAL",
            (false, false, true, false) => "SEAMLESS",
            (false,  false, false, true) => "SEAMLESS",
            _ => throw new ArgumentException("Unknown order type: " + duration)
        };

    private async Task<FSharpResult<T,ServiceError>> CallApi<T>(UserState user, string function, HttpMethod method, string? jsonData = null, bool debug = false)
    {
        try
        {
            var (response, content) = await CallApiWithoutSerialization(user, function, method, jsonData);

            if (debug)
            {
                _logger?.LogError("debug function: {function}", function);
                _logger?.LogError("debug response code: {statusCode}", response.StatusCode);
                _logger?.LogError("debug response output: {content}", content);
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = JsonSerializer.Deserialize<ErrorResponse>(content);
                if (error?.error == null)
                {
                    return ToFSharpError<T>(content);
                }

                return ToFSharpError<T>(error.error);
            }

            var deserialized = JsonSerializer.Deserialize<T>(content)
                ?? throw new Exception($"Could not deserialize response for {function}: {content}");
            return ToFSharpResult<T>(deserialized);
        }
        catch (TimeoutRejectedException e)
        {
            _logger?.LogError(e, "Failed to call {function}", function);

            return ToFSharpError<T>(e.Message);
        }
    }


    private async Task<(HttpResponseMessage response, string content)> CallApiWithoutSerialization(UserState user, string function, HttpMethod method, string? jsonData = null)
    {
        var oauth = await GetAccessToken(user);

        var tuple = await _wrappedPolicy.ExecuteAsync(
            async ct => {
                var request = new HttpRequestMessage(method, GenerateApiUrl(function));

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", oauth.access_token);
                if (jsonData != null)
                {
                    request.Content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                }
                var response = await _httpClient.SendAsync(request, ct);
                var content = await response.Content.ReadAsStringAsync(ct);

                // NOTE: even though I have rate limiting set above, and it's throttled to be
                // under documented TD Ameritrade limits, I still get this error sometimes.
                // so I am capturing it here and simulating rate limit exception 
                // so that polly can deal with it (it should wait and retry)
                // if (content.Contains("Individual App's transactions per seconds restriction reached"))
                // {
                //     throw new RateLimitRejectedException(
                //         retryAfter: TimeSpan.FromSeconds(1),
                //         message: "Transaction limit reached."
                //     );
                // }

                return (response, content);
            },
            CancellationToken.None
        );

        return tuple;
    }

    private async Task<OAuthResponse> RefreshAccessTokenInternal(UserState user, bool fullRefresh)
    {
        var postData = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", user.BrokerageRefreshToken },
            { "client_id", _clientId }
        };

        if (fullRefresh)
        {
            postData.Add("access_type", "offline");
        }

        var content = new FormUrlEncodedContent(postData);

        var response = await _httpClient.PostAsync(GenerateApiUrl("/oauth2/token"), content);

        LogIfFailed(response, "refresh access token");

        var responseString = await response.Content.ReadAsStringAsync();

        var deserialized = JsonSerializer.Deserialize<OAuthResponse>(responseString) ??
                           throw new Exception("Could not deserialize access token: " + responseString);
        return deserialized;
    }

    public Task<OAuthResponse> RefreshAccessToken(UserState user) =>
        RefreshAccessTokenInternal(user, fullRefresh: true);

    private readonly AsyncLock _asyncLock = new AsyncLock();
    
    public async Task<OAuthResponse> GetAccessToken(UserState user)
    {
        if (!user.ConnectedToBrokerage)
        {
            throw new Exception("User is not connected to brokerage");
        }
        
        // go to the storage to check for access token there
        var storageKey = "access-token:" + user.Id;
        var token = await _blogStorage.Get<OAuthResponse>(storageKey);
        
        if (token is { IsExpired: false })
        {
            return token;
        }

        using (await _asyncLock.LockAsync())
        {
            // check again, in case another thread has already refreshed the token
            token = await _blogStorage.Get<OAuthResponse>(storageKey);
            if (token is { IsExpired: false })
            {
                return token;
            }
            
            _logger?.LogInformation("Refreshing access token");
            
            token = await RefreshAccessTokenInternal(user, fullRefresh: false);
            token.created = FSharpOption<DateTimeOffset>.Some(DateTimeOffset.UtcNow);
            if (token.IsError)
            {
                _logger?.LogError("Could not refresh access token: {error}", token.error);
                throw new Exception("Could not refresh access token: " + token.error);
            }

            _logger?.LogInformation("Saving access token to storage");
        
            await _blogStorage.Save(storageKey, token);
            return token;
        }
    }

    private static string GenerateApiUrl(string function) => $"{ApiUrl}/{function}";
}

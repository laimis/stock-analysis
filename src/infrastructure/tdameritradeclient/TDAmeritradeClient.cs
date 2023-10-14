using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using core.Account;
using core.fs.Shared.Adapters.Brokerage;
using core.Shared;
using core.Shared.Adapters.Options;
using core.Shared.Adapters.Stocks;
using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Core;
using Polly;
using Polly.RateLimit;
using Polly.Retry;
using Polly.Timeout;

namespace tdameritradeclient;
public class TDAmeritradeClient : IBrokerage
{
    private readonly ILogger<TDAmeritradeClient>? _logger;
    private readonly string _callbackUrl;
    private readonly string _clientId;

    // in memory dictionary of access tokens (they expire every 30 mins)
    private readonly ConcurrentDictionary<Guid, OAuthResponse> _accessTokens = new();

    private const string ApiUrl = "https://api.tdameritrade.com/v1";
    private const string AuthUrl = "https://auth.tdameritrade.com";

    private readonly HttpClient _httpClient;
    private static readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<RateLimitRejectedException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(retryAttempt * 2)
        );

    private static readonly AsyncRateLimitPolicy _rateLimit = Policy.RateLimitAsync(
        numberOfExecutions: 20,
        perTimeSpan: TimeSpan.FromSeconds(1),
        maxBurst: 10);

    private static readonly AsyncTimeoutPolicy _timeoutPolicy = Policy.TimeoutAsync(
        seconds: (int)TimeSpan.FromSeconds(15).TotalSeconds
    );

    private readonly AsyncPolicy _wrappedPolicy = Policy.WrapAsync(
        _retryPolicy, _rateLimit, _timeoutPolicy
    );

    public TDAmeritradeClient(ILogger<TDAmeritradeClient>? logger, string callbackUrl, string clientId)
    {
        _logger = logger;
        _callbackUrl = callbackUrl;
        _clientId = clientId;
        _httpClient = new HttpClient();
    }

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

    public async Task<ServiceResponse<StockProfile>> GetStockProfile(UserState state, Ticker ticker)
    {
        var function = $"instruments?symbol={ticker.Value}&projection=fundamental";

        var results = await CallApi<Dictionary<string, SearchItemWithFundamental>>(
            state, function, HttpMethod.Get
        );

        if (results.Error != null)
        {
            return new ServiceResponse<StockProfile>(results.Error);
        }

        var fundamentals = results.Success![ticker.Value].fundamental ?? new Dictionary<string, object>();
        var data = results.Success[ticker.Value];

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


        return new ServiceResponse<StockProfile>(mapped);
    }

    public async Task<ServiceResponse<SearchResult[]>> Search(UserState state, string query, int limit)
    {
        var function = $"instruments?symbol={query}.*&projection=symbol-regex";

        var results = await CallApi<Dictionary<string, SearchItem>>(state, function, HttpMethod.Get);
        if (results.Error != null)
        {
            return new ServiceResponse<SearchResult[]>(results.Error);
        }

        var converted = results.Success!.Values.Select(
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

        return new ServiceResponse<SearchResult[]>(converted);
    }

    public async Task<ServiceResponse<TradingAccount>> GetAccount(UserState user)
    {
        var response = await CallApi<AccountsResponse[]>(
            user,
            "/accounts?fields=positions,orders",
            HttpMethod.Get
        );
            
        if (response.Error != null)
        {
            return new ServiceResponse<TradingAccount>(response.Error);
        }

        var accounts = response.Success!;

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

        var account = new TradingAccount
        {
            Orders = orders,
            StockPositions = stockPositions,
            OptionPositions = optionPositions,
            CashBalance = accounts[0].securitiesAccount?.currentBalances?.cashBalance
        };

        return new ServiceResponse<TradingAccount>(account);
    }

    public async Task<ServiceResponse<bool>> CancelOrder(UserState user, string orderId)
    {
        // get account first
        var response = await CallApi<AccountsResponse[]>(user, "/accounts", HttpMethod.Get);

        if (response.Error != null)
        {
            return new ServiceResponse<bool>(response.Error);
        }

        var accounts = response.Success!;

        var accountId = accounts[0].securitiesAccount?.accountId;

        var url = $"/accounts/{accountId}/orders/{orderId}";

        var (cancelResponse, content) = await CallApiWithoutSerialization(user, url, HttpMethod.Delete);
        return cancelResponse.IsSuccessStatusCode switch
        {
            true => new ServiceResponse<bool>(true),
            false => new ServiceResponse<bool>(new ServiceError(content))
        };
    }

    public Task<ServiceResponse<bool>> BuyOrder(
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

    public Task<ServiceResponse<bool>> SellOrder(
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

    public async Task<ServiceResponse<StockQuote>> GetQuote(UserState user, Ticker ticker)
    {
        var function = $"marketdata/{ticker.Value}/quotes";

        var response = await CallApi<Dictionary<string, StockQuote>>(user, function, HttpMethod.Get);
        if (!response.IsOk)
        {
            return new ServiceResponse<StockQuote>(response.Error!);
        }

        if (!response.Success!.TryGetValue(ticker, out var quote))
        {
            return new ServiceResponse<StockQuote>(new ServiceError("Could not find quote for ticker"));
        }

        return new ServiceResponse<StockQuote>(quote);
    }

    public Task<ServiceResponse<Dictionary<string, StockQuote>>> GetQuotes(UserState user, IEnumerable<Ticker> tickers)
    {
        var function = $"marketdata/quotes?symbol={string.Join(",", tickers.Select(t => t.Value))}";

        return CallApi<Dictionary<string, StockQuote>>(user, function, HttpMethod.Get);
    }

    public async Task<ServiceResponse<core.Shared.Adapters.Options.OptionChain>> GetOptions(UserState state, Ticker ticker, FSharpOption<DateTimeOffset> expirationDate, FSharpOption<decimal> strikePrice, FSharpOption<string> contractType)
    {
        var function = $"marketdata/chains?symbol={ticker.Value}";

        if (FSharpOption<string>.None.Equals(contractType) == false)
        {
            function += $"&contractType={contractType}";
        }

        if (FSharpOption<DateTimeOffset>.None.Equals(expirationDate) == false)
        {
            function += $"&fromDate={expirationDate.Value:yyyy-MM-dd}";
            function += $"&toDate={expirationDate.Value:yyyy-MM-dd}";
        }

        if (FSharpOption<decimal>.None.Equals(strikePrice) == false)
        {
            function += $"&strike={strikePrice.Value}";
        }

        var chainResponse = await CallApi<OptionChain>(state, function, HttpMethod.Get);

        if (!chainResponse.IsOk)
        {
            return new ServiceResponse<core.Shared.Adapters.Options.OptionChain>(chainResponse.Error!);
        }

        static IEnumerable<OptionDetail> ToOptionDetails(Dictionary<string, OptionDescriptorMap> map, decimal? underlyingPrice) =>
            map.SelectMany(kp => kp.Value.Values).SelectMany(v => v)
            .Select(d => new OptionDetail(symbol: d.symbol!, side: d.putCall?.ToLower()!, description: d.description!) {
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

        var chain = chainResponse.Success!;

        var response = new core.Shared.Adapters.Options.OptionChain(
            symbol: chain.symbol!,
            volatility: chain.volatility,
            numberOfContracts: chain.numberOfContracts,
            options: ToOptionDetails(chain.callExpDateMap!, chain.underlyingPrice).Union(ToOptionDetails(chain.putExpDateMap!, chain.underlyingPrice)).ToArray()
        );

        return new ServiceResponse<core.Shared.Adapters.Options.OptionChain>(response);
    }

    public async Task<ServiceResponse<MarketHours>> GetMarketHours(UserState state, DateTimeOffset date)
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        var function = $"marketdata/EQUITY/hours?date={dateStr}";

        var wrapper = await CallApi<MarketHoursWrapper>(state, function, HttpMethod.Get, jsonData: null);
        if (!wrapper.IsOk)
        {
            return new ServiceResponse<MarketHours>(wrapper.Error!);
        }

        if (wrapper.Success!.equity == null)
        {
            return new ServiceResponse<MarketHours>(new ServiceError("Could not find market hours for date"));
        }

        if (wrapper.Success.equity.EQ == null)
        {
            return new ServiceResponse<MarketHours>(new ServiceError("Could not find market hours for date (EQ)"));
        }

        return new ServiceResponse<MarketHours>(wrapper.Success.equity.EQ);
    }


    public async Task<ServiceResponse<PriceBar[]>> GetPriceHistory(
        UserState state,
        Ticker ticker,
        PriceFrequency frequency,
        DateTimeOffset start,
        DateTimeOffset end)
    {
        var startUnix = start == DateTimeOffset.MinValue ? DateTimeOffset.UtcNow.AddYears(-2).ToUnixTimeMilliseconds() : start.ToUnixTimeMilliseconds();
        var endUnix = end == DateTimeOffset.MinValue ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : end.ToUnixTimeMilliseconds();

        var frequencyType = frequency switch {
            PriceFrequency.Daily => "daily",
            PriceFrequency.Weekly => "weekly",
            PriceFrequency.Monthly => "monthly",
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, null)
        };
        
        var function = $"marketdata/{ticker.Value}/pricehistory?periodType=month&frequencyType={frequencyType}&startDate={startUnix}&endDate={endUnix}";

        var response = await CallApi<PriceHistoryResponse>(
            state,
            function,
            HttpMethod.Get
        );

        if (response.Error != null)
        {
            return new ServiceResponse<PriceBar[]>(response.Error);
        }

        var prices = response.Success!;

        if (prices.candles == null)
        {
            throw new Exception($"Null candles for historical prices for {ticker} {start} {end}");
        }

        var payload = prices.candles.Select(c => new PriceBar(
            close: c.close,
            high: c.high,
            low: c.low,
            open: c.open,
            date: DateTimeOffset.FromUnixTimeMilliseconds(c.datetime),
            volume: c.volume
        )).ToArray();

        if (payload.Length == 0)
        {
            _logger?.LogError("No candles for historical prices for {function}", function);
        }

        return new ServiceResponse<PriceBar[]>(payload);
    }

    private async Task<ServiceResponse<bool>> EnterOrder(UserState user, object postData)
    {
        var response = await CallApi<AccountsResponse[]>(user, "/accounts", HttpMethod.Get);

        if (response.Error != null)
        {
            return new ServiceResponse<bool>(response.Error);
        }

        var accounts = response.Success!;

        var accountId = accounts[0].securitiesAccount?.accountId;

        var url = $"/accounts/{accountId}/orders";

        var data = JsonSerializer.Serialize(postData);

        var (enterResponse, content) = await CallApiWithoutSerialization(user, url, HttpMethod.Post, data);
        
        return enterResponse.IsSuccessStatusCode switch
        {
            true => new ServiceResponse<bool>(true),
            false => new ServiceResponse<bool>(new ServiceError(content))
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

    private async Task<ServiceResponse<T>> CallApi<T>(UserState user, string function, HttpMethod method, string? jsonData = null, bool debug = false)
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
                    return new ServiceResponse<T>(new ServiceError(content));
                }

                return new ServiceResponse<T>(new ServiceError(error.error));
            }

            var deserialized = JsonSerializer.Deserialize<T>(content)
                ?? throw new Exception($"Could not deserialize response for {function}: {content}");
            return new ServiceResponse<T>(deserialized);
        }
        catch (TimeoutRejectedException e)
        {
            _logger?.LogError(e, "Failed to call {function}", function);

            return new ServiceResponse<T>(new ServiceError(e.Message));
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
                //         message: "Individual App's transactions per seconds restriction reached"
                //     );
                // }

                return (response, content);
            },
            CancellationToken.None
        );

        

        return tuple;
    }

    public async Task<OAuthResponse> GetAccessToken(UserState user)
    {
        if (!_accessTokens.TryGetValue(user.Id, out OAuthResponse? value) || value.IsExpired)
        {
            value = await RefreshAccessToken(user);
            _accessTokens[user.Id] = value;
        }
        return value;
    }

    private async Task<OAuthResponse> RefreshAccessToken(UserState user)
    {
        var postData = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", user.BrokerageRefreshToken },
            { "client_id", _clientId }
        };

        var content = new FormUrlEncodedContent(postData);

        var response = await _httpClient.PostAsync(GenerateApiUrl("/oauth2/token"), content);

        LogIfFailed(response, "refresh access token");

        var responseString = await response.Content.ReadAsStringAsync();

        var deserialized = JsonSerializer.Deserialize<OAuthResponse>(responseString) ??
            throw new Exception("Could not deserialize access token: " + responseString);
        return deserialized;
    }

    private static string GenerateApiUrl(string function) => $"{ApiUrl}/{function}";
}

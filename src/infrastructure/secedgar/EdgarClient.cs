using core.fs;
using core.fs.Adapters.SEC;
using core.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Core;
using SecuritiesExchangeCommission.Edgar;

namespace secedgar;
public class EdgarClient : ISECFilings
{
    private readonly ILogger<EdgarClient>? _logger;

    public EdgarClient(ILogger<EdgarClient>? logger) : this(logger, "NGTD/1.0"){}
    

    public EdgarClient(ILogger<EdgarClient>? logger, string userAgent)
    {
        _logger = logger;
        SecRequestManager.Instance.UserAgent = userAgent;
    }
        

    public async Task<FSharpResult<CompanyFilings,ServiceError>> GetFilings(Ticker symbol)
    {
        try
        {
            var results = await EdgarSearch.CreateAsync(
                stock_symbol: symbol.Value,
                results_per_page: EdgarSearchResultsPerPage.Entries10
            );

            var filings = new List<CompanyFiling>();

            foreach(var r in results.Results)
            {
                var filing = new CompanyFiling(r.Description,
                    r.DocumentsUrl,
                    r.FilingDate,
                    r.Filing,
                    r.InteractiveDataUrl
                );

                filings.Add(filing);
            }

            var result = new CompanyFilings(symbol, filings);

            return FSharpResult<CompanyFilings, ServiceError>.NewOk(result);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting SEC filings for {symbol}", symbol);

            return FSharpResult<CompanyFilings, ServiceError>.NewError(
                new ServiceError(ex.Message)
            );
        }
    }
}

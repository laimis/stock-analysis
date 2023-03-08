using core.Shared;
using core.Shared.Adapters.SEC;
using Microsoft.Extensions.Logging;
using SecuritiesExchangeCommission.Edgar;

namespace secedgar;
public class EdgarClient : ISECFilings
{
    private ILogger<EdgarClient>? _logger;

    public EdgarClient(ILogger<EdgarClient>? logger) : this(logger, "NGTD/1.0"){}
    

    public EdgarClient(ILogger<EdgarClient>? logger, string userAgent)
    {
        _logger = logger;
        SecRequestManager.Instance.UserAgent = userAgent;
    }
        

    public async Task<ServiceResponse<CompanyFilings>> GetFilings(string symbol)
    {
        try
        {
            var results = await EdgarSearch.CreateAsync(
                stock_symbol: symbol
            );

            var filings = new List<CompanyFiling>();

            foreach(var r in results.Results)
            {
                var filing = new CompanyFiling {
                    Description = r.Description,
                    DocumentsUrl = r.DocumentsUrl,
                    FilingDate = r.FilingDate,
                    Filing = r.Filing,
                    InteractiveDataUrl = r.InteractiveDataUrl,
                };

                filings.Add(filing);
            }

            var result = new CompanyFilings(symbol, filings);

            return new ServiceResponse<CompanyFilings>(result);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting SEC filings for {symbol}", symbol);

            return new ServiceResponse<CompanyFilings>(
                new ServiceError(ex.Message)
            );
        }
    }
}

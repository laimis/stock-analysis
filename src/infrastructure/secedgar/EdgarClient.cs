using core.Shared.Adapters.SEC;
using SecuritiesExchangeCommission.Edgar;

namespace secedgar;
public class EdgarClient
{
    public EdgarClient(string userAgent) => 
        SecRequestManager.Instance.UserAgent = userAgent;

    public async Task<List<CompanyFiling>> GetCompanyFilingsAsync(string symbol)
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
                // Details = details
            };

            filings.Add(filing);
        }

        return filings;
    }
}

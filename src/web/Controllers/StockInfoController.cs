using System;
using System.Threading.Tasks;
using core.fs.Accounts;
using dbstockinfoprovider;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

namespace web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockInfoController(DbStockInfoProvider provider) : ControllerBase
{
    public class QuoteData
    {
        public string Ticker { get; set; }
        public decimal LastPrice { get; set; }
        public DateTimeOffset LastUpdate { get; set; }
    }

    public class HistoricalData
    {
        public string Ticker { get; set; }
        public DateTimeOffset Date { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Volume { get; set; }
    }
    
    [HttpGet("quoterequests")]
    public async Task<IActionResult> GetQuoteRequests()
    {
        var result = await provider.GetQuoteRequests();
        return this.OkOrError(result);
    }
    
    [HttpPost("quotes")]
    public async Task<IActionResult> SubmitQuote([FromBody] QuoteData quoteData)
    {
        try
        {
            await provider.WriteQuoteData(quoteData.Ticker, quoteData.LastPrice, quoteData.LastUpdate);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("historical")]
    public async Task<IActionResult> SubmitHistoricalData([FromBody] HistoricalData historicalData)
    {
        try
        {
            await provider.WriteHistoricalData(historicalData.Ticker, historicalData.Date, historicalData.Open, historicalData.High, historicalData.Low, historicalData.Close, historicalData.Volume);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

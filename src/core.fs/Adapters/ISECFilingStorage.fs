namespace core.fs.Adapters.Storage

open System
open System.Collections.Generic
open System.Threading.Tasks
open core.Shared
open core.fs.Adapters.SEC

[<CLIMutable>]
type SECFilingRecord =
    {
        Id: Guid
        Ticker: string
        Cik: string
        FormType: string
        FilingDate: string
        ReportDate: string option
        Description: string
        FilingUrl: string
        DocumentUrl: string
        CreatedAt: DateTimeOffset
        IsXBRL: bool
        IsInlineXBRL: bool
    }

type ISECFilingStorage =
    /// Store a new SEC filing (ignores duplicates based on filing URL)
    abstract member SaveFiling : filing:SECFilingRecord -> Task<bool>
    
    /// Store multiple SEC filings (ignores duplicates based on filing URL).
    /// Returns only the filings that were actually inserted (new rows); duplicates are excluded.
    abstract member SaveFilings : filings:seq<SECFilingRecord> -> Task<IEnumerable<SECFilingRecord>>
    
    /// Get all filings for a ticker, ordered by filing date descending
    abstract member GetFilingsByTicker : ticker:Ticker -> Task<IEnumerable<SECFilingRecord>>
    
    /// Get recent filings for a ticker (within last N days)
    abstract member GetRecentFilingsByTicker : ticker:Ticker -> days:int -> Task<IEnumerable<SECFilingRecord>>
    
    /// Get filings by multiple tickers (for portfolio view)
    abstract member GetFilingsByTickers : tickers:seq<Ticker> -> days:int -> Task<IEnumerable<SECFilingRecord>>
    
    /// Check if a filing already exists by URL
    abstract member FilingExists : filingUrl:string -> Task<bool>
    
    /// Get filings by form type(s)
    abstract member GetFilingsByFormType : formTypes:seq<string> -> limit:int -> Task<IEnumerable<SECFilingRecord>>

    /// Get filings by form type(s) that have no corresponding ownership events.
    /// Used by catch-up parsing jobs to find filings that failed or were missed.
    /// TODO: Add a limit and a filing_date cutoff parameter to bound runtime and prevent
    /// unbounded full-table scans as the sec_filings table grows.
    abstract member GetFilingsWithoutOwnershipEvents : formTypes:seq<string> -> Task<IEnumerable<SECFilingRecord>>

    /// Get filings for a ticker created after the given timestamp, ordered by created_at ascending
    abstract member GetFilingsSince : ticker:Ticker -> since:DateTimeOffset -> Task<IEnumerable<SECFilingRecord>>

    /// Get the notification watermark timestamp for a user+ticker pair
    abstract member GetWatermark : userId:string -> ticker:Ticker -> Task<DateTimeOffset option>

    /// Upsert the notification watermark for a user+ticker pair
    abstract member UpsertWatermark : userId:string -> ticker:Ticker -> timestamp:DateTimeOffset -> Task<unit>

module SECFilingRecord =
    /// Convert a CompanyFiling to SECFilingRecord
    let fromCompanyFiling (ticker: Ticker) (cik: string) (filing: CompanyFiling) : SECFilingRecord =
        {
            Id = Guid.NewGuid()
            Ticker = ticker.Value
            Cik = cik
            FormType = filing.Filing
            FilingDate = filing.FilingDate
            ReportDate = filing.ReportDate
            Description = filing.Description
            FilingUrl = filing.FilingUrl
            DocumentUrl = filing.DocumentUrl
            CreatedAt = DateTimeOffset.UtcNow
            IsXBRL = filing.IsXBRL
            IsInlineXBRL = filing.IsInlineXBRL
        }
    
    /// Convert SECFilingRecord back to CompanyFiling
    let toCompanyFiling (record: SECFilingRecord) : CompanyFiling =
        {
            Description = record.Description
            DocumentUrl = record.DocumentUrl
            FilingDate = record.FilingDate
            ReportDate = record.ReportDate
            Filing = record.FormType
            FilingUrl = record.FilingUrl
            IsXBRL = record.IsXBRL
            IsInlineXBRL = record.IsInlineXBRL
        }

namespace storage.postgres

open System
open System.Collections.Generic
open System.Threading.Tasks
open System.Linq
open Dapper
open Npgsql
open core.Shared
open core.fs.Adapters.Storage

type SECFilingStorage(connectionString: string) =
    
    let getConnection () = new NpgsqlConnection(connectionString)
    
    interface ISECFilingStorage with
        
        member _.SaveFiling(filing: SECFilingRecord) : Task<bool> =
            task {
                use db = getConnection()
                
                let query = """
                    INSERT INTO sec_filings (id, ticker, cik, form_type, filing_date, report_date, description, filing_url, document_url, created_at, is_xbrl, is_inline_xbrl)
                    VALUES (@Id, @Ticker, @Cik, @FormType, @FilingDate, @ReportDate, @Description, @FilingUrl, @DocumentUrl, @CreatedAt, @IsXBRL, @IsInlineXBRL)
                    ON CONFLICT (filing_url) DO NOTHING"""
                
                let! rowsAffected = db.ExecuteAsync(query, filing)
                return rowsAffected > 0
            }
        
        member _.SaveFilings(filings: seq<SECFilingRecord>) : Task<int> =
            task {
                use db = getConnection()
                
                let query = """
                    INSERT INTO sec_filings (id, ticker, cik, form_type, filing_date, report_date, description, filing_url, document_url, created_at, is_xbrl, is_inline_xbrl)
                    VALUES (@Id, @Ticker, @Cik, @FormType, @FilingDate, @ReportDate, @Description, @FilingUrl, @DocumentUrl, @CreatedAt, @IsXBRL, @IsInlineXBRL)
                    ON CONFLICT (filing_url) DO NOTHING"""
                
                let filingsArray = filings.ToArray()
                let! rowsAffected = db.ExecuteAsync(query, filingsArray)
                return rowsAffected
            }
        
        member _.GetFilingsByTicker(ticker: Ticker) : Task<IEnumerable<SECFilingRecord>> =
            task {
                use db = getConnection()
                
                let query = """
                    SELECT id as Id, ticker as Ticker, cik as Cik, form_type as FormType, 
                           filing_date as FilingDate, report_date as ReportDate, description as Description,
                           filing_url as FilingUrl, document_url as DocumentUrl, created_at as CreatedAt, is_xbrl as IsXBRL, is_inline_xbrl as IsInlineXBRL
                    FROM sec_filings
                    WHERE ticker = @Ticker
                    ORDER BY filing_date DESC"""
                
                let! results = db.QueryAsync<SECFilingRecord>(query, {| Ticker = ticker.Value |})
                return results
            }
        
        member _.GetRecentFilingsByTicker(ticker: Ticker) (days: int) : Task<IEnumerable<SECFilingRecord>> =
            task {
                use db = getConnection()
                
                let cutoffDate = DateTimeOffset.UtcNow.AddDays(-float days).ToString("yyyy-MM-dd")
                
                let query = """
                    SELECT id as Id, ticker as Ticker, cik as Cik, form_type as FormType, 
                           filing_date as FilingDate, report_date as ReportDate, description as Description,
                           filing_url as FilingUrl, document_url as DocumentUrl, created_at as CreatedAt, is_xbrl as IsXBRL, is_inline_xbrl as IsInlineXBRL
                    FROM sec_filings
                    WHERE ticker = @Ticker AND filing_date >= @CutoffDate
                    ORDER BY filing_date DESC"""
                
                let! results = db.QueryAsync<SECFilingRecord>(query, {| Ticker = ticker.Value; CutoffDate = cutoffDate |})
                return results
            }
        
        member _.GetFilingsByTickers(tickers: seq<Ticker>) (days: int) : Task<IEnumerable<SECFilingRecord>> =
            task {
                let tickerValues = tickers |> Seq.map (fun t -> t.Value) |> Seq.toArray
                
                if tickerValues.Length = 0 then
                    return Enumerable.Empty<SECFilingRecord>()
                else
                    use db = getConnection()
                    
                    let cutoffDate = DateTimeOffset.UtcNow.AddDays(-float days).ToString("yyyy-MM-dd")
                    
                    let query = """
                        SELECT id as Id, ticker as Ticker, cik as Cik, form_type as FormType, 
                               filing_date as FilingDate, report_date as ReportDate, description as Description,
                               filing_url as FilingUrl, document_url as DocumentUrl, created_at as CreatedAt, is_xbrl as IsXBRL, is_inline_xbrl as IsInlineXBRL
                        FROM sec_filings
                        WHERE ticker = ANY(@Tickers) AND filing_date >= @CutoffDate
                        ORDER BY ticker, filing_date DESC"""
                    
                    let! results = db.QueryAsync<SECFilingRecord>(query, {| Tickers = tickerValues; CutoffDate = cutoffDate |})
                    return results
            }
        
        member _.FilingExists(filingUrl: string) : Task<bool> =
            task {
                use db = getConnection()
                
                let query = "SELECT EXISTS(SELECT 1 FROM sec_filings WHERE filing_url = @FilingUrl)"
                
                let! exists = db.ExecuteScalarAsync<bool>(query, {| FilingUrl = filingUrl |})
                return exists
            }
        
        member _.GetFilingsByFormType(formTypes: seq<string>) (limit: int) : Task<IEnumerable<SECFilingRecord>> =
            task {
                let formTypesArray = formTypes.ToArray()
                
                if formTypesArray.Length = 0 then
                    return Enumerable.Empty<SECFilingRecord>()
                else
                    use db = getConnection()
                    
                    let query = """
                        SELECT id as Id, ticker as Ticker, cik as Cik, form_type as FormType, 
                               filing_date as FilingDate, report_date as ReportDate, description as Description,
                               filing_url as FilingUrl, document_url as DocumentUrl, created_at as CreatedAt, is_xbrl as IsXBRL, is_inline_xbrl as IsInlineXBRL
                        FROM sec_filings
                        WHERE form_type = ANY(@FormTypes)
                        ORDER BY filing_date DESC
                        LIMIT @Limit"""
                    
                    let! results = db.QueryAsync<SECFilingRecord>(query, {| FormTypes = formTypesArray; Limit = limit |})
                    return results
            }

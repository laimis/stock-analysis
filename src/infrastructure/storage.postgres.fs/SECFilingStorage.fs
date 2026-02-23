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
                
                let parameters = {|
                    Id = filing.Id
                    Ticker = filing.Ticker
                    Cik = filing.Cik
                    FormType = filing.FormType
                    FilingDate = filing.FilingDate
                    ReportDate = filing.ReportDate |> Option.toObj
                    Description = filing.Description
                    FilingUrl = filing.FilingUrl
                    DocumentUrl = filing.DocumentUrl
                    CreatedAt = filing.CreatedAt
                    IsXBRL = filing.IsXBRL
                    IsInlineXBRL = filing.IsInlineXBRL
                |}
                
                let! rowsAffected = db.ExecuteAsync(query, parameters)
                return rowsAffected > 0
            }
        
        member _.SaveFilings(filings: seq<SECFilingRecord>) : Task<int> =
            task {
                use db = getConnection()
                
                let query = """
                    INSERT INTO sec_filings (id, ticker, cik, form_type, filing_date, report_date, description, filing_url, document_url, created_at, is_xbrl, is_inline_xbrl)
                    VALUES (@Id, @Ticker, @Cik, @FormType, @FilingDate, @ReportDate, @Description, @FilingUrl, @DocumentUrl, @CreatedAt, @IsXBRL, @IsInlineXBRL)
                    ON CONFLICT (filing_url) DO NOTHING"""
                
                let parameters = 
                    filings 
                    |> Seq.map (fun filing -> {|
                        Id = filing.Id
                        Ticker = filing.Ticker
                        Cik = filing.Cik
                        FormType = filing.FormType
                        FilingDate = filing.FilingDate
                        ReportDate = filing.ReportDate |> Option.toObj
                        Description = filing.Description
                        FilingUrl = filing.FilingUrl
                        DocumentUrl = filing.DocumentUrl
                        CreatedAt = filing.CreatedAt
                        IsXBRL = filing.IsXBRL
                        IsInlineXBRL = filing.IsInlineXBRL
                    |})
                    |> Seq.toArray
                
                let! rowsAffected = db.ExecuteAsync(query, parameters)
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

        member _.GetFilingsWithoutOwnershipEvents(formTypes: seq<string>) : Task<IEnumerable<SECFilingRecord>> =
            task {
                let formTypesArray = formTypes.ToArray()

                if formTypesArray.Length = 0 then
                    return Enumerable.Empty<SECFilingRecord>()
                else
                    use db = getConnection()

                    let query = """
                        SELECT sf.id as Id, sf.ticker as Ticker, sf.cik as Cik, sf.form_type as FormType,
                               sf.filing_date as FilingDate, sf.report_date as ReportDate, sf.description as Description,
                               sf.filing_url as FilingUrl, sf.document_url as DocumentUrl, sf.created_at as CreatedAt,
                               sf.is_xbrl as IsXBRL, sf.is_inline_xbrl as IsInlineXBRL
                        FROM sec_filings sf
                        WHERE sf.form_type = ANY(@FormTypes)
                          AND NOT EXISTS (
                            SELECT 1 FROM ownership_events oe WHERE oe.filing_id = sf.id
                          )
                        ORDER BY sf.filing_date DESC"""

                    let! results = db.QueryAsync<SECFilingRecord>(query, {| FormTypes = formTypesArray |})
                    return results
            }

        member _.GetFilingsSince(ticker: Ticker) (since: DateTimeOffset) : Task<IEnumerable<SECFilingRecord>> =
            task {
                use db = getConnection()

                let query = """
                    SELECT id as Id, ticker as Ticker, cik as Cik, form_type as FormType, 
                           filing_date as FilingDate, report_date as ReportDate, description as Description,
                           filing_url as FilingUrl, document_url as DocumentUrl, created_at as CreatedAt, is_xbrl as IsXBRL, is_inline_xbrl as IsInlineXBRL
                    FROM sec_filings
                    WHERE ticker = @Ticker AND created_at > @Since
                    ORDER BY created_at ASC"""

                let! results = db.QueryAsync<SECFilingRecord>(query, {| Ticker = ticker.Value; Since = since |})
                return results
            }

        member _.GetWatermark(userId: string) (ticker: Ticker) : Task<DateTimeOffset option> =
            task {
                use db = getConnection()

                let query = """
                    SELECT last_notified_at
                    FROM user_sec_filing_watermarks
                    WHERE user_id = @UserId AND ticker = @Ticker"""

                let! results = db.QueryAsync<DateTime>(query, {| UserId = userId; Ticker = ticker.Value |})
                return results |> Seq.tryHead |> Option.map (fun dt -> DateTimeOffset(dt, TimeSpan.Zero))
            }

        member _.UpsertWatermark(userId: string) (ticker: Ticker) (timestamp: DateTimeOffset) : Task<unit> =
            task {
                use db = getConnection()

                let query = """
                    INSERT INTO user_sec_filing_watermarks (user_id, ticker, last_notified_at)
                    VALUES (@UserId, @Ticker, @Timestamp)
                    ON CONFLICT (user_id, ticker) DO UPDATE SET last_notified_at = @Timestamp"""

                let! _ = db.ExecuteAsync(query, {| UserId = userId; Ticker = ticker.Value; Timestamp = timestamp |})
                return ()
            }

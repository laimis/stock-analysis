# SEC Filing Ownership Tracking - Implementation Plan

**Created**: February 12, 2026  
**Status**: In Progress

## Overview
Add database backing for SEC filings and implement ownership tracking to monitor insider and institutional ownership changes over time.

## Current State
- ✅ EDGAR client for fetching filings
- ✅ Email alerts for new filings
- ✅ UI for viewing filings (search + portfolio view)
- ❌ No persistence (refetch every time)
- ❌ No deduplication (can send duplicate emails)
- ❌ No historical tracking
- ❌ No structured data extraction from forms

## Goal
Track insider and institutional ownership changes to:
- Identify significant buying/selling patterns
- Monitor institutional position changes
- Alert on meaningful ownership events
- Provide historical ownership timelines

---

## Phase 1: Foundation & Deduplication
**Goal**: Add database backing, eliminate duplicate emails, enable historical queries  
**Status**: ✅ **COMPLETED** (February 12, 2026)

### Tasks
- [x] **Database Schema**
  - [x] Create `sec_filings` table
    - Columns: id, ticker, cik, form_type, filing_date, report_date, description, filing_url, document_url, created_at
    - Unique constraint on filing_url (natural deduplication)
    - Index on ticker + filing_date
  
- [x] **Storage Layer** (F#)
  - [x] Create `ISECFilingStorage` interface in `core.fs/Adapters/ISECFilingStorage.fs`
  - [x] Implement PostgreSQL storage in `infrastructure/storage.postgres/SECFilingStorage.cs`
  - [x] Implement in-memory storage in `infrastructure/storage.memory/SECFilingStorage.cs`
  - [x] Add to DI registration in `DIHelper.cs`

- [x] **Monitoring Service Updates**
  - [x] Modify `SECFilingsMonitoringService.fs`:
    - Check database for existing filings before sending emails
    - Store new filings to database
    - Only send email for truly new filings (not in DB)
    - Get CIK from ticker mapping

- [ ] **API Endpoints** (optional for Phase 1)
  - [ ] GET `/api/sec/filings/{ticker}` - get stored filings for ticker
  - [ ] GET `/api/sec/filings/recent` - recent filings across all tickers

### Success Criteria
- ✅ No duplicate filing emails sent (deduplication via database)
- ✅ Historical filings queryable from database
- ✅ Monitoring service uses database as cache
- ✅ Tests pass (unit + integration) - Build successful

---

## Phase 2: Ownership Tracking & Parsing
**Goal**: Extract structured ownership data from specific form types

### Priority Form Types (in order)
1. **Form 4** - Insider transactions (after-the-fact)
   - Most common, easiest to parse, highest value
2. **Form 144** - Intent to sell restricted securities
   - You specifically mentioned this
3. **Form 13F** - Institutional holdings (quarterly)
   - Structured format, easier than 13D/G
4. **Schedule 13D/13G** - Large position disclosures (>5%)
   - Important but harder to parse consistently

### Tasks
- [ ] **Database Schema**
  - [ ] Create `ownership_events` table
    - Columns: id, filing_id (FK), ticker, entity_name, entity_type, transaction_type, shares, price_per_share, total_value, transaction_date, filing_date, created_at
    - entity_type: 'insider', 'institution', 'large_holder'
    - transaction_type: 'purchase', 'sale', 'holding'
    - Index on ticker + transaction_date
    - Index on entity_name
  
  - [ ] Create `ownership_entities` table (normalized)
    - Columns: id, name, entity_type, cik, first_seen, last_seen
    - Deduplicate similar entity names

- [ ] **Form Parsers** (F#)
  - [ ] Create `core.fs/SEC/FormParsers.fs` module
  - [ ] Implement Form 4 parser
    - Extract: reporter name, relationship, transaction date, shares, price, transaction code
  - [ ] Implement Form 144 parser
    - Extract: seller name, shares to be sold, date
  - [ ] Implement Form 13F parser
    - Extract: fund name, positions with share counts
  - [ ] Implement Schedule 13D/G parser
    - Extract: entity name, shares owned, percent of class

- [ ] **Processing Service**
  - [ ] Create `SECFilingProcessingService.fs`
    - Background job to process unprocessed filings
    - Downloads filing document
    - Runs appropriate parser
    - Stores extracted ownership events
    - Marks filing as processed

- [ ] **Storage Layer**
  - [ ] Add ownership event storage methods to storage interface
  - [ ] Implement PostgreSQL storage
  - [ ] Implement in-memory storage

### Success Criteria
- ✅ Form 4 transactions extracted with >90% accuracy
- ✅ Ownership events stored in database
- ✅ Processing job runs daily
- ✅ Tests for each parser with real-world examples

---

## Phase 3: UI & Analytics
**Goal**: Surface ownership insights to users

### Tasks
- [ ] **API Endpoints**
  - [ ] GET `/api/ownership/{ticker}` - ownership timeline
  - [ ] GET `/api/ownership/{ticker}/insiders` - insider transaction history
  - [ ] GET `/api/ownership/{ticker}/institutions` - institutional holder changes
  - [ ] GET `/api/ownership/alerts` - significant ownership changes

- [ ] **Frontend Components**
  - [ ] Ownership timeline chart (Chart.js)
  - [ ] Insider transaction table
  - [ ] Institutional holder table with change tracking
  - [ ] Add ownership tab to stock detail view

- [ ] **Enhanced Alerts**
  - [ ] Alert on insider buying clusters (3+ insiders in 30 days)
  - [ ] Alert on >10% institutional position change
  - [ ] Alert on director selling >50% of holdings
  - [ ] Add ownership alerts to email templates

- [ ] **Analytics**
  - [ ] Correlate insider buying with future price movement (backtesting)
  - [ ] Track "smart money" institutions (best performing)
  - [ ] Identify stocks with increasing institutional interest

### Success Criteria
- ✅ Users can view ownership timeline for any ticker
- ✅ Ownership alerts integrated into email notifications
- ✅ UI performs well with historical data
- ✅ Analytics provide actionable insights

---

## Phase 4: Advanced Features (Future)
**Goal**: Leverage ownership data for trading signals

### Potential Features
- [ ] Insider buying/selling screener
- [ ] Institutional ownership momentum screen
- [ ] Form 4 sentiment score (aggregate insider activity)
- [ ] Ownership pattern recognition (pre-earnings buying, etc.)
- [ ] Integration with portfolio alerts (insider buying in your holdings)
- [ ] Whale watching (track specific funds/insiders across all stocks)

---

## Technical Considerations

### Parsing Challenges
- **Inconsistent formats**: SEC forms are HTML/XML but layout varies
- **Entity name variations**: "John A. Smith" vs "Smith, John A." vs "J.A. Smith"
- **Indirect ownership**: Options, derivatives, family trusts complicate share counts
- **Amendments**: Form 4/A amends previous filing - need to handle updates

### Solutions
- Start with most structured forms (Form 4)
- Use fuzzy matching for entity name deduplication
- Store raw filing text alongside parsed data
- Flag uncertain parses for manual review
- Build confidence scores for parsed data

### Performance
- Rate limiting: EDGAR allows 10 requests/second
- Caching: Store parsed results, don't reprocess
- Async processing: Don't block user requests on parsing
- Batch processing: Process multiple filings in background job

### Data Quality
- Log parsing failures with filing URL for debugging
- Add manual override capability for important filings
- Track parser accuracy metrics
- Implement data validation rules

---

## Migration Path

### Phase 1 Migration
1. No data migration needed (new tables)
2. Start fresh with current filings
3. Optional: Backfill historical filings for active portfolio tickers

### Phase 2 Migration
1. Process existing filings in `sec_filings` table
2. Start with most recent filings (last 90 days)
3. Gradually backfill older filings as needed

---

## Testing Strategy

### Unit Tests
- Form parsers with real-world examples (success + edge cases)
- Entity name normalization logic
- Deduplication logic
- Storage layer CRUD operations

### Integration Tests
- End-to-end filing fetch → store → parse → extract workflow
- Email service with database deduplication
- API endpoints returning stored data

### Data Quality Tests
- Parse 100 random Form 4s, manually verify sample
- Check for duplicate entities with similar names
- Validate share counts against known data sources

---

## Success Metrics

### Phase 1
- 0 duplicate filing emails sent
- 100% of fetched filings stored in database
- Database queries <100ms for ticker history

### Phase 2
- >90% parsing accuracy for Form 4
- >95% successful entity extraction
- Daily processing completes in <1 hour

### Phase 3
- Users spend >2 min on ownership timeline pages
- 50% reduction in user "research time" for ownership checks
- Positive user feedback on ownership alerts

---

## Current Phase: **Phase 2** 🚀
**Status**: Ready to start  
**Previous Phase**: Phase 1 Complete  
**Next Steps**: Design ownership_events schema and build Form 4 parser

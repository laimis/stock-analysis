# TradeWatch - AI Coding Agent Instructions

## Project Overview
TradeWatch is a stock, options, and crypto portfolio tracking application built with a **hybrid F#/C# architecture**. The system uses **event sourcing** for data persistence and supports both PostgreSQL and in-memory storage. The goal is to eventually eliminate the C# components, migrating all business logic to F# for improved type safety and maintainability. All new code should be written in F# unless there is a compelling reason to use C#.

## Architecture

### Polyglot .NET Structure
- **C# Components**: Domain aggregates, events, infrastructure adapters (in `src/core`)
- **F# Components**: Domain aggregates, events, Business logic, handlers, services, type-safe domain modeling (in `src/core.fs`)
- **Frontend**: Angular 20+ with TypeScript (in `src/frontend`)
- **Host**: ASP.NET Core 10.0 web application (in `src/web`)

### Key Projects
```
src/core           → C# domain aggregates (OwnedStock, StockList, etc.)
src/core.fs        → F# domain aggregates, business logic and handlers
src/web            → ASP.NET Core API and hosting
src/frontend       → Angular SPA
src/infrastructure → External service integrations (storage, APIs, messaging)
src/studies        → F# data analysis and backtesting scripts
src/migrations     → F# database migration utilities
```

## Critical Patterns

### Event Sourcing Architecture
- In C#, all domain entities inherit from `Aggregate<T>` (C#) and emit `AggregateEvent` objects:

```csharp
// C# defines the aggregate structure
public class OwnedStock : Aggregate<OwnedStockState>
{
    public void Purchase(decimal shares, decimal price, DateTimeOffset date) 
    {
        Apply(new StockPurchased_v2(...)); // Events stored, not state
    }
}
```

- In F#, aggregates follow a similar pattern but leverage F#'s discriminated unions and functional paradigms:
- Events are persisted to `events` table in PostgreSQL via `PostgresAggregateStorage`
- State is reconstructed by replaying events in aggregate constructors
- Never modify state directly; always emit events through `Apply()`
- When writing F# code, avoid using mutable state; prefer pure functions and immutable data structures

**Mark F# DTOs with `[<CLIMutable>]`** for JSON serialization compatibility

### Dependency Injection (src/infrastructure/di/DIHelper.cs)
- Central DI registration in `DIHelper.RegisterServices()`
- Storage mode controlled by environment variable `storage` ("postgres" or "memory")
- External API keys loaded from configuration (IEX, CoinMarketCap, Schwab, etc.)
- F# services registered alongside C# infrastructure services

### Angular Frontend Patterns
- Services in `src/frontend/src/app/services/` (e.g., `stocks.service.ts`, `stockpositions.service.ts`)
- Components use `inject()` function for dependency injection (Angular 14+ pattern)
- API calls return Observables; use `.subscribe()` or `async` pipe
- Chart.js for visualizations, Bootstrap 5 for styling

### Important information on frontend styles

- **CSS Classes**: Use as many built-in Bootstrap classes as possible
- **Custom CSS**: Define in `styles.css`, avoid inline styles or component-specific styles that are located in component css files
- **Component-specific styles**: Use sparingly, define in `styles.css` if necessary

## Developer Workflows

### Building and Testing
```powershell
# Run all tests (excludes integration/Postgres tests by default)
.\test.bat  # Or: dotnet test --filter "Category!=Integration&Category!=Postgres" --nologo

# Build entire solution
dotnet build tradewatch.sln
# Or use VS Code task: "build"

# Watch mode for live development
dotnet watch run --project src/web/web.csproj
# Or use VS Code task: "watch"
```

### Running Locally
1. **Set environment variables** (see `dev.bat` or create `dev_secret.bat`):
   ```bat
   set storage=memory
   set IEXToken=your_token_here
   set COINMARKETCAPToken=your_token_here
   ```
2. **For Postgres**: Set `DB_CNN` connection string and `storage=postgres`
3. **Run**: `dotnet run --project src/web/web.csproj`
4. Frontend served from `wwwroot/` (build Angular separately if developing UI)

### Deployment Workflow
- **Production release**: `.\release_prod.ps1 "commit message"`
  - Merges `main` → `prod` branch
  - Auto-increments version tag
  - Builds Angular in production mode
  - Publishes Docker image
- **Docker build**: Alpine-based, self-contained Linux binaries (see `Dockerfile`)
- **Hangfire** for background jobs (requires Postgres connection)

## External Integrations

### Stock Market Data
- **IEX Cloud API**: Stock prices, profiles, fundamentals (adapter in `core.fs/Adapters/Stocks.fs`)
- Token required in `IEXToken` config

### Brokerage
- **Schwab API**: Account sync, positions, orders (F# client in `infrastructure/schwabclient`)
- OAuth flow managed in `src/web/Controllers/BrokerageController.cs`

### Crypto
- **CoinMarketCap API**: Crypto prices and metadata (adapter in `infrastructure/coinmarketcap`)
- Token required in `COINMARKETCAPToken` config

### Notifications
- **SendGrid**: Email alerts (wrapper in `infrastructure/sendgridclient`)
- **Twilio**: SMS notifications (wrapper in `infrastructure/twilioclient`)
- Alert system in `core.fs/Alerts/MonitoringServices.fs`

### Other
- **SEC EDGAR**: Financial filings parsing (`infrastructure/secedgar`)
- **Hangfire**: Scheduled background jobs (daily alerts, price monitoring)

## Project-Specific Conventions

### Naming
- F# handlers: `*Handler.fs` (e.g., `StocksHandler`, `OptionsHandler`)
- F# services: `*Service.fs` or grouped in `Services/` folder
- C# aggregates: `Owned*`, `*List`, `Pending*` patterns (e.g., `OwnedStock`, `StockList`)
- Events: Past tense verbs (`StockPurchased`, `TickerObtained`)

### Testing
- xUnit for all tests
- Test projects mirror source structure: `coretests`, `coretests.fs`, `infrastructuretests`
- Use `Category` attribute to exclude slow integration tests: `[Trait("Category", "Integration")]`
- Test utilities in `tests/testutils`

### F# Studies and Notebooks
- `src/studies/*.fs`: Backtesting, screeners, breakout analysis
- Jupyter notebooks: `*.ipynb` for exploratory data analysis
- Run studies via PowerShell scripts (e.g., `breakout_study_2024.ps1`)

## Common Gotchas
1. **Always rebuild Angular before Docker**: Use `ng build --configuration production` in `src/frontend`
2. **F# project order matters**: `core.fs.fsproj` must reference `core.csproj` before compilation
3. **Postgres vs Memory storage**: Switch via `storage` env var; Postgres requires `DB_CNN`
4. **Hangfire needs Postgres**: Background jobs won't run with in-memory storage
5. **API rate limits**: IEX and CoinMarketCap have usage quotas; check logs for 429 errors
6. **Time zones**: Market hours logic in `infrastructure/timezonesupport` handles NYSE/NASDAQ calendars

## Getting Oriented Quickly
- **Domain logic**: Start in `src/core.fs/{Stocks,Options,Portfolio}/`
- **API endpoints**: Check `src/web/Controllers/`
- **Storage queries**: See `src/infrastructure/storage.postgres/`
- **Frontend features**: Explore `src/frontend/src/app/` by domain area
- **Background jobs**: Review `src/web/Utils/Jobs.cs` and F# `MonitoringServices.fs` files

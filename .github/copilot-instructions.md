# NGTDTrading - AI Coding Agent Instructions

## Project Overview
Nightingale Trading (aka NGTDTrading) is a stock, options, and crypto portfolio tracking application built with a **hybrid F#/C# architecture**. The system uses **event sourcing** for data persistence with PostgreSQL. The goal is to eventually eliminate the C# components, migrating all business logic to F# for improved type safety and maintainability. All new code should be written in F# unless there is a compelling reason to use C#.

## Architecture

### Onion Architecture
This project follows **onion architecture**. Dependencies always point inward — outer layers depend on inner layers, never the reverse:

1. **Core** (`src/core`, `src/core.fs`) — innermost ring; zero dependencies on infrastructure or web. Defines domain types, aggregates, events, and **port interfaces** (under `src/core.fs/Adapters/`) that outer layers must implement.
2. **Infrastructure** (`src/infrastructure/*`) — middle ring; implements the core's port interfaces. Each infra project depends only on `core.fs`/`core`, never on other infra projects (except `storage.shared.fs` as a shared utility). Includes storage, email, SMS, brokerage clients, SEC, CSV parsing, etc.
3. **Composition Root** (`src/infrastructure/di.fs`) — wires all infra implementations to core interfaces via DI; the only place allowed to reference all infra projects simultaneously.
4. **Web / Entry Point** (`src/web.fs`) — outermost ring; references only `core.fs` (for types/interfaces), `core` (transitional), `di.fs` (for DI wiring), and `web.interop`. Must **not** reference individual infra projects directly.

> When adding new features: define interfaces in `core.fs/Adapters/`, implement them in the appropriate `infrastructure/` project, register in `di.fs`, and consume via the interface in `web.fs` or `core.fs` handlers.

### Polyglot .NET Structure
- **C# Components**: Domain aggregates, events, infrastructure adapters (in `src/core`)
- **F# Components**: Domain aggregates, events, Business logic, handlers, services, type-safe domain modeling (in `src/core.fs`)
- **Frontend**: Angular 20+ with TypeScript (in `src/frontend`)
- **Host**: ASP.NET Core 10.0 web application (in `src/web.fs`)

### Key Projects
```
src/core           → C# domain aggregates (OwnedStock, StockList, etc.)
src/core.fs        → F# domain aggregates, business logic and handlers, app services
src/web.fs         → ASP.NET Core API and hosting
src/frontend       → Angular SPA
src/infrastructure → External service integrations (storage, APIs, messaging, emails)
src/studies        → F# data analysis and backtesting scripts
src/migrations     → F# database migration utilities
tests/*            → xUnit test projects for C# and F# code
```

## Critical Patterns

### UI Patterns

- **HTML Structure**: Use Bootstrap 5 components for consistency
- **CSS Classes**: Use as many built-in Bootstrap classes as possible
- **Custom CSS**: Define in `styles.css`, avoid inline styles or component-specific styles that are located in component css files
- **Component-specific styles**: Use sparingly, define in `styles.css` if absolutely necessary

### F# Coding Conventions
- **Never use `printfn`** in generated F# code — it is strictly forbidden
- Prefer `Console.WriteLine` and `Console.Write` for all console output
- Avoid mutable state; prefer pure functions and immutable data structures
- **Prefer functional collection operations** (`List.map`, `List.filter`, `List.fold`, `Seq.map`, `Array.map`, etc.) over imperative `for x in ... do` loops or `if/else` chains; use pattern matching and pipeline operators (`|>`) to express logic idiomatically

### Event Sourcing Architecture
- Aggregates follow a similar pattern but leverage F#'s discriminated unions and functional paradigms:
- Events are persisted to `events` table in PostgreSQL via `PostgresAggregateStorage`
- State is reconstructed by replaying events in aggregate constructors
- Never modify state directly; always emit events through `Apply()`
- When writing F# code, avoid using mutable state; prefer pure functions and immutable data structures

### Dependency Injection (src/infrastructure/di/DIHelper.cs)
- Central DI registration in `DIHelper.RegisterServices()`
- Storage requires environment variable `storage` set to "postgres"
- External API keys loaded from configuration (IEX, CoinMarketCap, Schwab, etc.)
- F# services registered alongside C# infrastructure services

### Angular Frontend Patterns
- Services in `src/frontend/src/app/services/` (e.g., `stocks.service.ts`, `stockpositions.service.ts`)
- Components use `inject()` function for dependency injection (Angular 14+ pattern)
- API calls return Observables; use `.subscribe()` or `async` pipe
- Chart.js for visualizations, Bootstrap 5 for styling

## Developer Workflows

### Building and Testing
```powershell
# Run all tests (excludes integration/Postgres tests by default)
.\test.bat  # Or: dotnet test --filter "Category!=Integration&Category!=Postgres" --nologo

# Build entire solution
dotnet build tradewatch.sln
# Or use VS Code task: "build"

# Watch mode for live development
dotnet watch run --project src/web.fs/web.fs.fsproj
# Or use VS Code task: "watch"
```

### Running Locally
1. **Set environment variables** (see `dev.bat` or create `dev_secret.bat`):
   ```bat
   set storage=postgres
   set DB_CNN=Server=...;Database=...;User id=...;password=...
   set IEXToken=your_token_here
   set COINMARKETCAPToken=your_token_here
   ```
2. **Run**: `dotnet run --project src/web.fs/web.fs.fsproj`
3. Frontend served from `wwwroot/` (build Angular separately if developing UI)

### Deployment Workflow
- **Production release**: `.\release_prod.ps1 "commit message"`
  - Merges `main` → `prod` branch
  - Auto-increments version tag
  - Builds Angular in production mode
  - Publishes Docker image
- **Docker build**: Alpine-based, self-contained Linux binaries (see `Dockerfile`)
- **Hangfire** for background jobs (requires Postgres connection)
- **When Asked To Deploy**: Commit local changes with descriptive message, then run `.\release_prod.ps1 "Your short description of the changes"`

## External Integrations

### Stock Market Data
- **Schwab API**: Stock prices, profiles, fundamentals (adapter in `core.fs/Adapters/Stocks.fs`)

### Brokerage
- **Schwab API**: Account sync, positions, orders (F# client in `infrastructure/schwabclient`)
- OAuth flow managed in `src/web.fs/Controllers/BrokerageController.fs`

### Crypto
- **CoinMarketCap API**: Crypto prices and metadata (adapter in `infrastructure/coinmarketcap`)
- Token required in `COINMARKETCAPToken` config

### Notifications
- **AmazonSES**: Email alerts (wrapper in `infrastructure/emailclient`), email templates use scriban templates
- **Twilio**: SMS notifications (wrapper in `infrastructure/twilioclient`)
- Alert system in `core.fs/Alerts/MonitoringServices.fs`

### Other
- **SEC EDGAR**: Financial filings parsing (`infrastructure/secedgar`)
- **Hangfire**: Scheduled background jobs (daily alerts, price monitoring)

## F# Coding Style

- **No unnecessary parentheses on single-argument calls**: In F#, single-argument method and function calls do not require parentheses. Always omit them.
  - Correct: `logger.LogInformation "message"`
  - Wrong: `logger.LogInformation("message")`
  - Correct: `logger.LogError ex.Message`
  - Wrong: `logger.LogError(ex.Message)`
- **Parentheses are only required** when passing a tuple (multiple arguments to a .NET method), or when grouping an expression: `obj.Method(arg1, arg2)` or `foo (bar baz)`.
- This applies to all F# code across `src/core.fs/`, `src/infrastructure/`, `src/web.fs/`, `src/studies/`, and `src/migrations/`.

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
3. **Postgres required**: Application requires `DB_CNN` connection string and `storage=postgres`
4. **Hangfire needs Postgres**: Background jobs require Postgres connection
5. **Time zones**: Market hours logic in `infrastructure/timezonesupport` handles NYSE/NASDAQ calendars

## Getting Oriented Quickly
- **Domain logic**: Start in `src/core.fs/{Stocks,Options,Portfolio}/`
- **API endpoints**: Check `src/web.fs/Controllers/`
- **Storage queries**: See `src/infrastructure/storage.postgres/`
- **Frontend features**: Explore `src/frontend/src/app/` by domain area
- **Background jobs**: Review `src/web.interop/Jobs.cs` and F# `MonitoringServices.fs` files

### Updating Angular and .NET Components
- there is angular_update.ps1 script for updating Angular
- there is dotnet_update.ps1 script for updating .NET dependencies

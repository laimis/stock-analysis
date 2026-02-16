# C# to F# Migration Plan - NGTDTrading

## Overview
This document outlines the migration strategy for converting all C# projects (except `core.csproj`) to F#. The approach creates mirror F# projects alongside C# projects, implements functionality, then switches references in the web project.

## Migration Strategy

### Approach
1. **Create parallel F# project** (e.g., `storage.postgres.fs`)
2. **Implement F# equivalents** of all public APIs
3. **Ensure CLI compatibility** using `[<CLIMutable>]` for DTOs
4. **Update references** in consuming projects (mainly `di` and `web`)
5. **Run tests** to validate migration
6. **Remove C# project** after successful validation

### Projects Excluded from Migration
- `src/core/core.csproj` - Keep as-is (too many dependencies, will migrate later)
- `src/frontend/frontend.csproj` - Angular/TypeScript build project, not actual C# code

---

## Migration Tiers

### **TIER 1: Infrastructure Foundation** (No external dependencies)
These projects can be migrated in parallel as they have minimal dependencies on each other.

---

### 1. `storage.shared` → `storage.shared.fs`

**Project Location:** `src/infrastructure/storage.shared/`  
**New Location:** `src/infrastructure/storage.shared.fs/`

**Current Files:**
- `IAggregateStorage.cs` - Interface for event storage
- `IOutbox.cs` - Interface for outbox pattern
- `PortfolioStorage.cs` - Portfolio storage implementation
- `StoredAggregateEvent.cs` - Event storage model
- `EventInfraAdjustments.cs` - Infrastructure adjustments

**Dependencies:** 
- References `core.csproj` (keep this reference)
- No other internal dependencies

**LLM Prompt:**
```
I need to migrate the C# project `src/infrastructure/storage.shared` to F#. This project contains storage interfaces and shared types used across the event sourcing infrastructure.

Create a new F# project at `src/infrastructure/storage.shared.fs/storage.shared.fs.fsproj` that:

1. Defines the following interfaces and types in F#:
   - `IAggregateStorage` - Generic interface for aggregate event storage
   - `IOutbox` - Interface for implementing the outbox pattern
   - `StoredAggregateEvent` - Record type for stored events (mark with [<CLIMutable>])
   - `PortfolioStorage` - Portfolio-specific storage implementation
   - `EventInfraAdjustments` - Infrastructure adjustment helpers

2. Ensure all types are:
   - Marked with [<CLIMutable>] if they're DTOs/data classes
   - Exposed with proper namespace (keep `storage.shared` namespace)
   - CLI-compatible for C# interop

3. Reference `src/core/core.csproj` (not core.fs)

4. Use immutable F# patterns (records, discriminated unions) where appropriate

Please read the existing C# files in `src/infrastructure/storage.shared/` and create equivalent F# implementations that maintain the same public API surface.
```

---

### 2. `securityutils` → `securityutils.fs`

**Project Location:** `src/infrastructure/securityutils/`  
**New Location:** `src/infrastructure/securityutils.fs/`

**Current Files:**
- `PasswordHashProvider.cs` - Password hashing implementation (likely using BCrypt or similar)

**Dependencies:** None (standalone utility)

**LLM Prompt:**
```
I need to migrate the C# project `src/infrastructure/securityutils` to F#. This project provides password hashing functionality.

Create a new F# project at `src/infrastructure/securityutils.fs/securityutils.fs.fsproj` that:

1. Provides F# implementation of `PasswordHashProvider`:
   - Hash password function
   - Verify password function
   - Use same hashing algorithm as C# version for compatibility

2. Ensure the API is:
   - CLI-compatible for C# consumers
   - Maintains same public interface
   - Uses F# functional patterns (pure functions where possible)

3. No internal project dependencies

Please read the existing C# file `src/infrastructure/securityutils/PasswordHashProvider.cs` and create an equivalent F# implementation.
```

---

### 3. `timezonesupport` → `timezonesupport.fs`

**Project Location:** `src/infrastructure/timezonesupport/`  
**New Location:** `src/infrastructure/timezonesupport.fs/`

**Current Files:**
- `MarketHours.cs` - NYSE/NASDAQ market hours logic, holiday calendar

**Dependencies:** None (standalone utility)

**LLM Prompt:**
```
I need to migrate the C# project `src/infrastructure/timezonesupport` to F#. This project handles market hours and trading calendar logic for NYSE/NASDAQ.

Create a new F# project at `src/infrastructure/timezonesupport.fs/timezonesupport.fs.fsproj` that:

1. Implements `MarketHours` functionality in F#:
   - Market open/close times
   - Trading day validation
   - Holiday calendar
   - Timezone conversions (likely EST/EDT for US markets)

2. Use F# types:
   - Records for configuration
   - DateTimeOffset for timezone-aware dates
   - Pure functions where possible

3. Ensure CLI compatibility for C# consumers

Please read the existing C# file `src/infrastructure/timezonesupport/MarketHours.cs` and create an equivalent F# implementation that maintains the same public API.
```

---

### 4. `csvparser` → `csvparser.fs`

**Project Location:** `src/infrastructure/csvparser/`  
**New Location:** `src/infrastructure/csvparser.fs/`

**Current Files:**
- `CSVParser.cs` - CSV parsing logic
- `CsvWriterImpl.cs` - CSV writing implementation

**Dependencies:** Likely uses CsvHelper NuGet package

**LLM Prompt:**
```
I need to migrate the C# project `src/infrastructure/csvparser` to F#. This project provides CSV parsing and writing functionality.

Create a new F# project at `src/infrastructure/csvparser.fs/csvparser.fs.fsproj` that:

1. Implements CSV functionality in F#:
   - CSV parsing (reading CSV files into records)
   - CSV writing (writing records to CSV format)
   - Likely wraps CsvHelper library

2. Use F# types:
   - Records with [<CLIMutable>] for CSV DTOs
   - Sequence/list processing for rows
   - Type-safe column mapping

3. Ensure CLI compatibility for C# consumers

Please read the existing C# files in `src/infrastructure/csvparser/` and create equivalent F# implementations.
```

---

### 5. `coinmarketcap` → `coinmarketcap.fs`

**Project Location:** `src/infrastructure/coinmarketcap/`  
**New Location:** `src/infrastructure/coinmarketcap.fs/`

**Current Files:**
- `CoinMarketCapClient.cs` - HTTP client for CoinMarketCap API

**Dependencies:** HTTP client libraries

**LLM Prompt:**
```
I need to migrate the C# project `src/infrastructure/coinmarketcap` to F#. This project provides integration with the CoinMarketCap API for cryptocurrency price data.

Create a new F# project at `src/infrastructure/coinmarketcap.fs/coinmarketcap.fs.fsproj` that:

1. Implements CoinMarketCap API client in F#:
   - HTTP requests to CoinMarketCap endpoints
   - JSON deserialization to F# records
   - API key authentication
   - Rate limiting/error handling

2. Use F# async workflows or tasks for HTTP operations

3. Define response types as records with [<CLIMutable>]

4. Ensure CLI compatibility

Please read `src/infrastructure/coinmarketcap/CoinMarketCapClient.cs` and create an equivalent F# implementation. Use F# Data Providers or System.Text.Json for JSON handling.
```

---

### 6. `twilioclient` → `twilioclient.fs`

**Project Location:** `src/infrastructure/twilioclient/`  
**New Location:** `src/infrastructure/twilioclient.fs/`

**Current Files:**
- `TwilioClientWrapper.cs` - Wrapper around Twilio SDK for SMS

**Dependencies:** Twilio NuGet package

**LLM Prompt:**
```
I need to migrate the C# project `src/infrastructure/twilioclient` to F#. This project wraps the Twilio SDK for sending SMS notifications.

Create a new F# project at `src/infrastructure/twilioclient.fs/twilioclient.fs.fsproj` that:

1. Implements Twilio client wrapper in F#:
   - Send SMS functionality
   - Wraps Twilio SDK
   - Configuration for API credentials

2. Use F# async/task patterns for API calls

3. Ensure CLI compatibility for DI registration

Please read `src/infrastructure/twilioclient/TwilioClientWrapper.cs` and create an equivalent F# implementation.
```

---

### **TIER 2: Storage & Configuration** (Depends on Tier 1)

---

### 7. `storage.postgres` → `storage.postgres.fs`

**Project Location:** `src/infrastructure/storage.postgres/`  
**New Location:** `src/infrastructure/storage.postgres.fs/`

**Current Files:**
- `PostgresAggregateStorage.cs` - Main event storage implementation
- `AccountStorage.cs` - Account-specific queries
- `OwnershipStorage.cs` - Ownership tracking queries
- `SECFilingStorage.cs` - SEC filing storage

**Dependencies:**
- `storage.shared` (will change to `storage.shared.fs`)
- Npgsql for PostgreSQL
- Dapper for queries

**LLM Prompt:**
```
I need to migrate the C# project `src/infrastructure/storage.postgres` to F#. This project implements PostgreSQL-based event sourcing storage using the event sourcing pattern.

Create a new F# project at `src/infrastructure/storage.postgres.fs/storage.postgres.fs.fsproj` that:

1. Implements all storage classes in F#:
   - `PostgresAggregateStorage` - Core event storage implementing `IAggregateStorage`
   - `AccountStorage` - Account queries
   - `OwnershipStorage` - Ownership tracking
   - `SECFilingStorage` - SEC filing storage

2. Use F# with:
   - Npgsql for PostgreSQL connections
   - Dapper or F# type providers for queries
   - Async/task workflows for database operations
   - Records with [<CLIMutable>] for query results

3. Reference `storage.shared.fs` (the F# version we'll create)

4. Ensure implementations match the interfaces from `storage.shared.fs`

Please read all C# files in `src/infrastructure/storage.postgres/` and create equivalent F# implementations. Pay special attention to the event sourcing pattern - events are stored as JSON in the `events` table and replayed to reconstruct aggregate state.
```

---

### 8. `di` → `di.fs`

**Project Location:** `src/infrastructure/di/`  
**New Location:** `src/infrastructure/di.fs/`

**Current Files:**
- `DIHelper.cs` - Central dependency injection registration

**Dependencies:** References almost all infrastructure projects

**LLM Prompt:**
```
I need to migrate the C# project `src/infrastructure/di` to F#. This is the central dependency injection configuration that registers all services for the application.

Create a new F# project at `src/infrastructure/di.fs/di.fs.fsproj` that:

1. Implements DI registration in F#:
   - Read configuration for storage provider, API keys
   - Register all infrastructure services (storage, clients, etc.)
   - Use Microsoft.Extensions.DependencyInjection
   - Export a single registration function callable from C# web host

2. Reference all F# infrastructure projects:
   - `storage.shared.fs`
   - `storage.postgres.fs`
   - `securityutils.fs`
   - `timezonesupport.fs`
   - `csvparser.fs`
   - `coinmarketcap.fs`
   - `twilioclient.fs`
   - `schwabclient` (already F#)
   - `emailclient` (already F#)
   - `secedgar.fs` (already F#)
   - `core.fs` (already F#)

3. Keep reference to `core.csproj` (still C#)

4. Ensure the registration function is CLI-compatible so it can be called from the C# web project

Please read `src/infrastructure/di/DIHelper.cs` and create an equivalent F# implementation. The key function should be something like `registerServices : IServiceCollection -> IConfiguration -> IServiceCollection`.
```

---

### **TIER 3: Web Host** (Depends on everything)

---

### 9. `web` - Update References Only

**Project Location:** `src/web/`  
**Action:** Update project references, DO NOT migrate to F#

**Current Dependencies (C#):**
- `core.csproj` (keep)
- `storage.postgres.csproj` → change to `storage.postgres.fs`
- `csvparser.csproj` → change to `csvparser.fs`
- `coinmarketcap.csproj` → change to `coinmarketcap.fs`
- `twilioclient.csproj` → change to `twilioclient.fs`
- `timezonesupport.csproj` → change to `timezonesupport.fs`
- `securityutils.csproj` → change to `securityutils.fs`
- `di.csproj` → change to `di.fs`

**LLM Prompt:**
```
Update the web project (`src/web/web.csproj`) to reference the new F# infrastructure projects instead of the C# versions.

Change the following ProjectReferences:
- storage.postgres.csproj → storage.postgres.fs.fsproj
- csvparser.csproj → csvparser.fs.fsproj
- coinmarketcap.csproj → coinmarketcap.fs.fsproj
- twilioclient.csproj → twilioclient.fs.fsproj
- timezonesupport.csproj → timezonesupport.fs.fsproj
- securityutils.csproj → securityutils.fs.fsproj
- di.csproj → di.fs.fsproj

Keep these references unchanged:
- core.csproj
- core.fs.fsproj
- schwabclient.fsproj (already F#)

After making these changes, build the solution to ensure all references resolve correctly.
```

---

### **TIER 4: Test Projects** (Can be done in parallel with source migration)

---

### 10. Test Projects Migration

For each test project, create a corresponding `.fs` version:

| C# Test Project | F# Test Project | Tests |
|----------------|-----------------|-------|
| `tests/testutils` | `tests/testutils.fs` | Test utilities |
| `tests/securityutilstests` | `tests/securityutilstests.fs` | Password hashing tests |
| `tests/infrastructuretests/timezonesupporttests` | `tests/infrastructuretests/timezonesupporttests.fs` | Market hours tests |
| `tests/infrastructuretests/csvparsertests` | `tests/infrastructuretests/csvparsertests.fs` | CSV parsing tests |
| `tests/infrastructuretests/clienttests` | `tests/infrastructuretests/clienttests.fs` | Twilio/CoinMarketCap tests |
| `tests/storagetests` | `tests/storagetests.fs` | Storage tests |
| `tests/coretests` | Keep as-is | Tests core.csproj (not migrating yet) |

**Generic Test Migration LLM Prompt:**
```
I need to migrate the test project `[PROJECT_PATH]` to F#. 

Create a new F# test project at `[PROJECT_PATH].fs/[PROJECT_NAME].fs.fsproj` that:

1. Uses xUnit test framework (same as C# tests)
2. References the corresponding F# implementation project
3. Converts all tests to F# using:
   - xUnit `[<Fact>]` and `[<Theory>]` attributes
   - F# Arrange-Act-Assert pattern
   - FSUnit or Expecto for fluent assertions (optional)

4. Maintain test coverage - convert all existing tests

5. Use `[<Trait("Category", "Integration")>]` for integration tests

Please read all test files in `[PROJECT_PATH]/` and create equivalent F# test implementations.
```

---

## Migration Execution Order

### Phase 1: Foundation (Do first, in parallel)
1. `storage.shared.fs`
2. `securityutils.fs`
3. `timezonesupport.fs`
4. `csvparser.fs`
5. `coinmarketcap.fs`
6. `twilioclient.fs`

**Validation:** Build each project independently, run any unit tests

---

### Phase 2: Storage Layer
7. `storage.postgres.fs` (depends on `storage.shared.fs`)
8. `storagetests.fs` (tests for storage.postgres.fs)

**Validation:** Run integration tests against Postgres

---

### Phase 3: Dependency Injection
9. `di.fs` (depends on all Tier 1 & 2 projects)

**Validation:** Ensure all services can be registered

---

### Phase 4: Web Integration
10. Update `web.csproj` references to point to F# projects
11. Update solution file to include all new F# projects

**Validation:** Build entire solution, run `test.bat`

---

### Phase 5: Test Projects (Can overlap with Phase 1-3)
12. Migrate all test projects to F#

**Validation:** Full test suite passes

---

## Project File Template

When creating F# projects, use this template:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <!-- F# source files in compilation order -->
    <Compile Include="Types.fs" />
    <Compile Include="Implementation.fs" />
  </ItemGroup>

  <ItemGroup>
    <!-- Project references -->
    <ProjectReference Include="path/to/dependency.fsproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- NuGet packages -->
    <PackageReference Include="FSharp.Core" Version="9.0.100" />
  </ItemGroup>

</Project>
```

---

## Solution File Updates

After creating all F# projects, update `tradewatch.sln` to include them:

```
dotnet sln add src/infrastructure/storage.shared.fs/storage.shared.fs.fsproj
dotnet sln add src/infrastructure/storage.postgres.fs/storage.postgres.fs.fsproj
dotnet sln add src/infrastructure/securityutils.fs/securityutils.fs.fsproj
dotnet sln add src/infrastructure/timezonesupport.fs/timezonesupport.fs.fsproj
dotnet sln add src/infrastructure/csvparser.fs/csvparser.fs.fsproj
dotnet sln add src/infrastructure/coinmarketcap.fs/coinmarketcap.fs.fsproj
dotnet sln add src/infrastructure/twilioclient.fs/twilioclient.fs.fsproj
dotnet sln add src/infrastructure/di.fs/di.fs.fsproj
dotnet sln add tests/storagetests.fs/storagetests.fs.fsproj
dotnet sln add tests/testutils.fs/testutils.fs.fsproj
dotnet sln add tests/securityutilstests.fs/securityutilstests.fs.fsproj
dotnet sln add tests/infrastructuretests/timezonesupporttests.fs/timezonesupporttests.fs.fsproj
dotnet sln add tests/infrastructuretests/csvparsertests.fs/csvparsertests.fs.fsproj
dotnet sln add tests/infrastructuretests/clienttests.fs/clienttests.fs.fsproj
```

---

## Testing Strategy

After each phase:

1. **Build Validation:**
   ```powershell
   dotnet build tradewatch.sln
   ```

2. **Unit Tests:**
   ```powershell
   .\test.bat  # Runs all non-integration tests
   ```

3. **Integration Tests:**
   ```powershell
   .\test_database.bat  # Runs Postgres integration tests
   ```

4. **Full QA Testing:**
   - Start application: `dotnet run --project src/web/web.csproj`
   - Test all endpoints
   - Verify background jobs (Hangfire)
   - Test data import/export
   - Verify external API integrations (Schwab, CoinMarketCap, Twilio)

---

## Rollback Strategy

Keep C# projects in the solution until F# equivalents are fully validated. If issues arise:

1. Revert project references in `web.csproj` back to C# versions
2. Rebuild solution
3. Debug F# implementation issues
4. Re-test

Do NOT delete C# projects until:
- All tests pass
- Full QA cycle complete
- Application runs in production successfully for at least one release cycle

---

## Key F# Patterns to Use

### 1. DTOs - Use [<CLIMutable>]
```fsharp
[<CLIMutable>]
type StockPrice = {
    Ticker: string
    Price: decimal
    Date: DateTimeOffset
}
```

### 2. Interfaces - Use object expressions
```fsharp
type IAggregateStorage =
    abstract member Save : string -> Event list -> Async<unit>
    abstract member Load : string -> Async<Event list>
```

### 3. HTTP Clients - Use async
```fsharp
let fetchPrice ticker = async {
    use client = new HttpClient()
    let! response = client.GetStringAsync(url) |> Async.AwaitTask
    return parseJson response
}
```

### 4. Database - Use Dapper or FSharp.Data.SqlClient
```fsharp
let loadEvents aggregateId = async {
    use conn = new NpgsqlConnection(connectionString)
    do! conn.OpenAsync() |> Async.AwaitTask
    let! events = conn.QueryAsync<StoredEvent>(sql, {| Id = aggregateId |}) |> Async.AwaitTask
    return events |> Seq.toList
}
```

---

## Success Criteria

Migration is complete when:

- [ ] All Tier 1 projects migrated and building
- [ ] All Tier 2 projects migrated and building
- [ ] DI project registers all F# services
- [ ] Web project references F# projects and builds
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Application runs locally successfully
- [ ] Full QA testing complete
- [ ] Application deploys and runs in production
- [ ] C# projects can be safely removed from solution

---

## Notes

- **Do NOT migrate `core.csproj`** - This is explicitly excluded due to tight coupling with event sourcing aggregates
- **Do NOT migrate `frontend.csproj`** - This is just an Angular build wrapper
- **Keep F# and C# projects side-by-side** during migration for safe rollback
- **Update references incrementally** - don't try to flip everything at once
- **Test frequently** - validate after each phase
- **Use CLI-compatible types** - F# code must be callable from C#

---


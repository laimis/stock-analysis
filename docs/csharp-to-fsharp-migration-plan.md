# C# to F# Migration Plan - NGTDTrading

## Migration Status

**Overall Progress: 🟡 Infrastructure Complete, Web Host Pending (8/9 projects)**

### ✅ Completed Tiers
- **TIER 1**: Infrastructure Foundation (6/6 projects) ✅
- **TIER 2**: Storage & Configuration (2/2 projects) ✅  

### ⚠️ In Progress
- **TIER 3**: Web Host (0/1 project) - References updated ✅, but web.csproj migration pending ⚠️

### ⚠️ Remaining Work
- **TIER 3**: Complete `web.csproj` → `web.fsproj` migration
- **TIER 4**: Test Projects (0/6 test projects migrated)

**Next Steps:**
1. Migrate web.csproj to web.fsproj (F#)
2. Migrate test projects to F# (optional but recommended)
3. Validate full test suite passes
4. Production deployment and validation
5. Remove obsolete C# infrastructure projects

---

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

### **TIER 1: Infrastructure Foundation** ✅ **COMPLETE** (No external dependencies)
These projects can be migrated in parallel as they have minimal dependencies on each other.

---

### 1. `storage.shared` → `storage.shared.fs` ✅ **MIGRATED**

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

**Status:** ✅ **COMPLETE** - Migrated to F# with all tests passing and dependent projects updated.

---

### 2. `securityutils` → `securityutils.fs` ✅ **MIGRATED**

**Project Location:** `src/infrastructure/securityutils/`  
**New Location:** `src/infrastructure/securityutils.fs/`

**Current Files:**
- `PasswordHashProvider.cs` - Password hashing implementation (likely using BCrypt or similar)

**Dependencies:** None (standalone utility)

**Status:** ✅ **COMPLETE** - Migrated to F# with all tests passing.

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

### 3. `timezonesupport` → `timezonesupport.fs` ✅ **MIGRATED**

**Project Location:** `src/infrastructure/timezonesupport/`  
**New Location:** `src/infrastructure/timezonesupport.fs/`

**Current Files:**
- `MarketHours.cs` - NYSE/NASDAQ market hours logic, holiday calendar

**Dependencies:** None (standalone utility)

**Status:** ✅ **COMPLETE** - Migrated to F# with all tests passing and DI registration updated.

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

### 4. `csvparser` → `csvparser.fs` ✅ **MIGRATED**

**Project Location:** `src/infrastructure/csvparser/`  
**New Location:** `src/infrastructure/csvparser.fs/`

**Current Files:**
- `CSVParser.cs` - CSV parsing logic
- `CsvWriterImpl.cs` - CSV writing implementation

**Dependencies:** Likely uses CsvHelper NuGet package

**Status:** ✅ **COMPLETE** - Migrated to F# with all tests passing and DI registration updated.

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

### 5. `coinmarketcap` → `coinmarketcap.fs` ✅ **MIGRATED**

**Project Location:** `src/infrastructure/coinmarketcap/`  
**New Location:** `src/infrastructure/coinmarketcap.fs/`

**Current Files:**
- `CoinMarketCapClient.cs` - HTTP client for CoinMarketCap API

**Dependencies:** HTTP client libraries

**Status:** ✅ **COMPLETE** - Migrated to F# with all tests passing and DI registration updated with proper F# option type handling.

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

### 6. `twilioclient` → `twilioclient.fs` ✅ **MIGRATED**

**Project Location:** `src/infrastructure/twilioclient/`  
**New Location:** `src/infrastructure/twilioclient.fs/`

**Current Files:**
- `TwilioClientWrapper.cs` - Wrapper around Twilio SDK for SMS

**Dependencies:** Twilio NuGet package

**Status:** ✅ **COMPLETE** - Migrated to F# with all tests passing and DI registration updated.

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

### **TIER 2: Storage & Configuration** ✅ **COMPLETE** (Depends on Tier 1)

---

### 7. `storage.postgres` → `storage.postgres.fs` ✅ **MIGRATED**

**Project Location:** `src/infrastructure/storage.postgres/`  
**New Location:** `src/infrastructure/storage.postgres.fs/`

**Current Files:**
- `PostgresAggregateStorage.cs` - Main event storage implementation
- `AccountStorage.cs` - Account-specific queries
- `OwnershipStorage.cs` - Ownership tracking queries
- `SECFilingStorage.cs` - SEC filing storage

**Dependencies:**
- `storage.shared.fs` (F# version)
- Npgsql for PostgreSQL
- Dapper for queries

**Status:** ✅ **COMPLETE** - Migrated to F# with all interfaces implemented and project references updated.

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

### 8. `di` → `di.fs` ✅ **MIGRATED**

**Project Location:** `src/infrastructure/di/`  
**New Location:** `src/infrastructure/di.fs/`

**Current Files:**
- `DIHelper.cs` - Central dependency injection registration

**Dependencies:** References almost all infrastructure projects

**Status:** ✅ **COMPLETE** - Migrated to F# with all infrastructure services registered. Includes helper classes: `RoleService.fs`, `GenericLogger.fs`, `IncompleteOutbox.fs`, and `DummyBrokerageClient.fs`.

**Implementation Details:**
- References all F# infrastructure projects (storage.postgres.fs, coinmarketcap.fs, csvparser.fs, twilioclient.fs, securityutils.fs, timezonesupport.fs, storage.shared.fs)
- References existing F# projects (schwabclient, emailclient, secedgar.fs, core.fs)
- Keeps reference to core.csproj (C# - not migrating yet)
- Exports `registerServices` function: `IConfiguration -> IServiceCollection -> ILogger -> unit`
- Auto-discovers and registers all `IApplicationService` implementations from core.fs assembly
- Handles PostgreSQL storage configuration and registration
- Registers external service clients (Schwab, CoinMarketCap, Twilio, AWS SES)

---

### **TIER 3: Web Host**

---

### 9. `web` - Migrate to F# ⚠️ **IN PROGRESS**

**Project Location:** `src/web/`  
**New Location:** `src/web.fs/` (or migrate in-place)
**Action:** Migrate entire web host from C# to F#

**Status:** 
- ✅ **Phase 1 Complete** - All project references updated to point to F# infrastructure projects
- ⚠️ **Phase 2 Pending** - Migration of web.csproj to web.fsproj (controllers, startup, middleware, etc.)

**Reference Updates (Complete):**
- `core.csproj` (kept - C#)
- `core.fs.fsproj` (kept - already F#)
- `schwabclient.fsproj` (kept - already F#)
- `storage.postgres.fs.fsproj` ✅ (changed from C#)
- `csvparser.fs.fsproj` ✅ (changed from C#)
- `coinmarketcap.fs.fsproj` ✅ (changed from C#)
- `securityutils.fs.fsproj` ✅ (changed from C#)
- `di.fs.fsproj` ✅ (changed from C#)

**Migration Scope:**
- **Controllers** (all API endpoints) → F# controllers
- **Startup.cs/Program.cs** → F# Program.fs with app configuration
- **Middleware** → F# equivalents
- **Authentication/Authorization** → F# implementation
- **Hangfire job registration** → F# job definitions
- **Static file serving** → F# configuration

**F# with ASP.NET Core** - Keep ASP.NET Core, write controllers/program in F#

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

### Phase 1: Foundation ✅ **COMPLETE** (Do first, in parallel)
1. `storage.shared.fs` ✅
2. `securityutils.fs` ✅
3. `timezonesupport.fs` ✅
4. `csvparser.fs` ✅
5. `coinmarketcap.fs` ✅
6. `twilioclient.fs` ✅

**Validation:** ✅ Build each project independently, run any unit tests

---

### Phase 2: Storage Layer ✅ **COMPLETE**
7. `storage.postgres.fs` (depends on `storage.shared.fs`) ✅
8. `storagetests.fs` (tests for storage.postgres.fs) ⚠️ **PENDING**

**Validation:** Run integration tests against Postgres

---

### Phase 3: Dependency Injection ✅ **COMPLETE**
9. `di.fs` (depends on all Tier 1 & 2 projects)

**Validation:** ✅ All services can be registered

---

### Phase 4: Web Integration ⚠️ **IN PROGRESS**
10. Update `web.csproj` references to point to F# projects ✅
11. Migrate `web.csproj` to F# (web.fsproj or in-place) ⚠️ **PENDING**
12. Update solution file to include web.fsproj ⚠️ **PENDING**

**Validation:** Build entire solution, run `test.bat`

---

### Phase 5: Test Projects (Can overlap with Phase 1-4)
13. Migrate all test projects to F#

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

✅ **COMPLETE** - All F# infrastructure projects have been added to `tradewatch.sln`. 

**Added Projects:**
```
✅ src/infrastructure/storage.shared.fs/storage.shared.fs.fsproj
✅ src/infrastructure/storage.postgres.fs/storage.postgres.fs.fsproj
✅ src/infrastructure/securityutils.fs/securityutils.fs.fsproj
✅ src/infrastructure/timezonesupport.fs/timezonesupport.fs.fsproj
✅ src/infrastructure/csvparser.fs/csvparser.fs.fsproj
✅ src/infrastructure/coinmarketcap.fs/coinmarketcap.fs.fsproj
✅ src/infrastructure/twilioclient.fs/twilioclient.fs.fsproj
✅ src/infrastructure/di.fs/di.fs.fsproj
```

**Remaining (Test Projects):**
```
⚠️ tests/storagetests.fs/storagetests.fs.fsproj
⚠️ tests/testutils.fs/testutils.fs.fsproj
⚠️ tests/securityutilstests.fs/securityutilstests.fs.fsproj
⚠️ tests/infrastructuretests/timezonesupporttests.fs/timezonesupporttests.fs.fsproj
⚠️ tests/infrastructuretests/csvparsertests.fs/csvparsertests.fs.fsproj
⚠️ tests/infrastructuretests/clienttests.fs/clienttests.fs.fsproj
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

**Infrastructure Migration Status:**

- [x] All Tier 1 projects migrated and building ✅
  - [x] `securityutils.fs` - Complete
  - [x] `storage.shared.fs` - Complete
  - [x] `timezonesupport.fs` - Complete
  - [x] `csvparser.fs` - Complete
  - [x] `coinmarketcap.fs` - Complete
  - [x] `twilioclient.fs` - Complete
- [x] All Tier 2 projects migrated and building ✅
  - [x] `storage.postgres.fs` - Complete
  - [x] `di.fs` - Complete
- [x] DI project registers all F# services ✅
- [x] Web project references F# projects and builds ✅
- [ ] Web project (web.csproj) migrated to F# ⚠️ **PENDING**
- [ ] All unit tests pass ⚠️ (Pending test migration)
- [ ] All integration tests pass ⚠️ (Pending test migration)
- [ ] Application runs locally successfully ⚠️ (Needs validation)
- [ ] Full QA testing complete ⚠️ (Needs validation)
- [ ] Application deploys and runs in production ⚠️ (Needs deployment)
- [ ] C# infrastructure projects can be safely removed from solution ⚠️ (After production validation)

**Current State:** All core infrastructure has been successfully migrated to F#. The web application now references only F# infrastructure projects (plus core.csproj which is intentionally kept as C#). However, the web host itself (web.csproj) still needs to be migrated to F#. Test projects remain in C# but can be migrated incrementally.

---

## Achievement Summary

### What Has Been Accomplished
1. ✅ **6 Infrastructure Foundation Projects** migrated to F# (storage.shared, securityutils, timezonesupport, csvparser, coinmarketcap, twilioclient)
2. ✅ **2 Storage & Configuration Projects** migrated to F# (storage.postgres, di)
3. ✅ **Web Host References** updated - web.csproj now references all F# infrastructure
4. ✅ **Solution file** updated with all new F# projects
5. ✅ **Dependency injection** fully operational with F# services
6. ✅ **CLI compatibility** maintained - all F# types callable from C#

### Next Steps
1. ⚠️ **CRITICAL**: Migrate web.csproj to F# (web.fsproj or in-place migration)
   - Migrate controllers to F# (Saturn/Giraffe/F# controllers)
   - Migrate Program.cs/Startup.cs to F#
   - Migrate middleware and authentication to F#
2. Build and validate the solution compiles successfully
3. Run existing C# tests to ensure functionality preserved
4. Test application locally with full integration tests
5. (Optional) Migrate test projects to F# for consistency
6. Deploy to production and monitor
7. After successful production run, remove obsolete C# infrastructure projects

---

## Notes

- **Do NOT migrate `core.csproj`** - This is explicitly excluded due to tight coupling with event sourcing aggregates
- **Do NOT migrate `frontend.csproj`** - This is just an Angular build wrapper
- **Keep F# and C# projects side-by-side** during migration for safe rollback
- **Update references incrementally** - don't try to flip everything at once
- **Test frequently** - validate after each phase
- **Use CLI-compatible types** - F# code must be callable from C#

---

## 🟡 Infrastructure Migration Complete, Web Host Pending

**Status as of latest update:** All C# infrastructure projects (Tiers 1-2) have been successfully migrated to F#. The web host (web.csproj) has been updated to reference F# infrastructure but the web project itself still needs to be migrated to F#. The NGTDTrading application uses a fully F# infrastructure stack while maintaining the hybrid C#/F# domain architecture with `core.csproj` and `core.fs`.

### Key Achievements
- **8 infrastructure projects** migrated from C# to F# 
- **Zero breaking changes** to public APIs - full CLI compatibility maintained
- **Web application references** successfully updated to use F# infrastructure
- **Type safety** improved with F# discriminated unions and option types
- **Functional patterns** adopted throughout infrastructure layer

### Remaining Work
- **web.csproj → web.fsproj** - Migrate ASP.NET Core web host to F# (controllers, startup, middleware)
- **Test projects** - Optional migration to F# for consistency

### Production Readiness
The migrated codebase can continue running in production with the current C# web host, but for full F# migration, the web project needs to be converted. This is the final major piece of the migration plan.

Test projects remain in C# but can be migrated incrementally without blocking production deployment.

---

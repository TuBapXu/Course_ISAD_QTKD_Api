# QTKD.Api

**Contract & Business Pricing Management System** (*Hệ thống Quản lý Hợp đồng & Giá cước kinh doanh*)

A RESTful Web API built with ASP.NET Core to manage customers, contracts, and price lists. This is a solo academic project I built for learning purposes as part of my Information Systems coursework, with a focus on clean layered architecture, centralized error handling, and secure configuration management.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Npgsql-4169E1?logo=postgresql&logoColor=white)
![EF Core](https://img.shields.io/badge/EF%20Core-Code--First-68217A)
![xUnit](https://img.shields.io/badge/Tests-xUnit%20%2B%20Moq-512BD4)
![Swagger](https://img.shields.io/badge/Swagger-OpenAPI-85EA2D?logo=swagger&logoColor=black)

---

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Key Technical Solutions](#key-technical-solutions)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Error Handling](#error-handling)
- [Testing (Week 07)](#testing-week-07)
- [Deliverables & Documentation](#deliverables--documentation)
- [Roadmap](#roadmap)

---

## Overview

QTKD.Api digitizes the core workflow of managing business contracts and their pricing:

- **Customers** – the parties who sign contracts.
- **Contracts** – agreements linked to customers.
- **Price Lists** – collections of pricing terms.
- **Price Items** – the individual line items inside a price list.

The project applies **Separation of Concerns** so that each layer has a single, well-defined responsibility, which keeps the codebase easy to read, test, and extend.

---

## Architecture

The solution follows a layered architecture. Requests flow top-down; each layer only talks to the one directly beneath it.

```
HTTP Request
    │
    ▼
┌──────────────┐
│ Controllers  │  Thin HTTP layer: receives requests, delegates to services
└──────┬───────┘
       ▼
┌──────────────┐
│  Services    │  Core business logic; throws exceptions on failure
└──────┬───────┘
       ▼
┌──────────────┐
│ Repositories │  Data access via Entity Framework Core
└──────┬───────┘
       ▼
┌──────────────┐
│ PostgreSQL   │
└──────────────┘
```

### Project Structure

```
QTKD.Api/
├── Controllers/     # Thin HTTP layer – no business logic here
├── Services/        # Central business logic; throws exceptions on errors
├── Repositories/    # Data access layer built on EF Core
├── Data/            # QtkdDbContext, EF Core configuration, seed data
├── Entities/        # Customer, Contract, PriceList, PriceItem
├── DTOs/            # Request/response Data Transfer Objects
├── Exceptions/      # GlobalExceptionMiddleware & custom exceptions
├── docs/            # Week 06 & Week 07 reports and TRX test evidence
├── tests/
│   └── QTKD.Tests/  # xUnit + Moq automated test project
│       ├── UnitTests/         # Business rule unit tests (CTR-01, CTR-02, CTR-03)
│       └── IntegrationTests/  # API + QtkdTestDb tests (TC01–TC08, AC01, E2E)
├── Program.cs       # Application entry point & service registration
└── .env             # Local secrets (NOT committed to Git)
```

| Folder | Responsibility |
|---|---|
| `Controllers/` | Accepts HTTP requests, calls the matching service, returns the response. Kept intentionally thin. |
| `Services/` | Holds the business rules. Throws exceptions when something goes wrong instead of returning error codes. |
| `Repositories/` | Encapsulates all database queries using Entity Framework Core. |
| `Data/` | Contains `QtkdDbContext`, entity configurations, and seed data (all seeded dates use `DateTimeKind.Utc`). |
| `Entities/` | Domain models: `Customer`, `Contract`, `PriceList`, `PriceItem`. |
| `DTOs/` | Input/output payload contracts separated from EF Core entities. |
| `Exceptions/` | Contains `GlobalExceptionMiddleware`, which catches exceptions globally and maps them to proper HTTP status codes. |
| `tests/QTKD.Tests/` | Separate xUnit test project containing isolated service unit tests (Moq) and HTTP/PostgreSQL integration tests (`QtkdTestDb`). |
| `docs/` | Stores Week 06–07 reports and automated `.trx` test result logs. |

---

## Tech Stack

| Technology | Purpose |
|---|---|
| **.NET 10.0 / ASP.NET Core Web API** | Application framework |
| **PostgreSQL + Npgsql** | Relational database and its .NET provider |
| **Entity Framework Core** | ORM, using the Code-First approach with migrations |
| **DotNetEnv** | Loads configuration from a `.env` file so the database password stays out of source control |
| **Swagger / OpenAPI** | Interactive API documentation and testing UI |
| **xUnit + Moq + WebApplicationFactory** | Automated unit testing and real HTTP + PostgreSQL integration testing |

---

## Key Technical Solutions

### 1. JSON Object Cycle (Infinite Loop) Fix

Entities reference each other (e.g., a parent and its children), which caused a *"possible object cycle was detected"* error during JSON serialization. Resolved by configuring the serializer to ignore cycles:

```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
```

### 2. PostgreSQL DateTime Compatibility

Npgsql 6+ is strict about `DateTime.Kind` and may throw when writing non-UTC values. To keep date handling compatible, the legacy timestamp behavior is enabled at startup:

```csharp
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
```

In addition, all seed data uses `DateTimeKind.Utc` to keep stored timestamps consistent.

### 3. Global Exception Handling

Rather than scattering `try/catch` blocks across controllers, `GlobalExceptionMiddleware` intercepts exceptions thrown from the service layer and converts them into standard HTTP responses (e.g., `400 Bad Request`, `404 Not Found`). This keeps controllers clean and error responses consistent.

### 4. Secure Configuration

Sensitive values such as the database connection string are loaded from a local `.env` file via **DotNetEnv** and are excluded from version control through `.gitignore`.

---

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/download/) (running locally or remotely)
- [EF Core CLI tools](https://learn.microsoft.com/ef/core/cli/dotnet):
  ```bash
  dotnet tool install --global dotnet-ef
  ```

### Installation

**1. Clone the repository**

```bash
git clone https://github.com/<your-username>/QTKD.Api.git
cd QTKD.Api
```

**2. Create the `.env` file**

In the project root (next to `Program.cs`), create a file named `.env`:

```env
DB_CONNECTION_STRING=Host=localhost;Port=5432;Database=qtkd_db;Username=postgres;Password=your_password
```

> ⚠️ Never commit this file. Make sure `.env` is listed in `.gitignore`.

**3. Create the database**

Apply the EF Core migrations to create the schema (and seed data):

```bash
dotnet ef database update
```

**4. Run the application**

```bash
dotnet run
```

**5. Open Swagger**

Navigate to the Swagger UI in your browser. The exact port is printed in the console when the app starts, for example:

```
https://localhost:<port>/swagger
```

---

## Configuration

| Variable | Description | Example |
|---|---|---|
| `DB_CONNECTION_STRING` | PostgreSQL connection string used by `QtkdDbContext` | `Host=localhost;Port=5432;Database=QtkdDb;Username=postgres;Password=...` |
| `TEST_DB_CONNECTION_STRING` *(optional)* | Override PostgreSQL connection string used by integration tests (defaults to `DB_CONNECTION_STRING` with database name automatically replaced by `QtkdTestDb`) | `Host=localhost;Port=5432;Database=QtkdTestDb;Username=postgres;Password=...` |

---

## Error Handling

All unhandled exceptions are processed by `GlobalExceptionMiddleware` and returned with a consistent status code:

| Status Code | Meaning | Typical Cause |
|---|---|---|
| `400 Bad Request` | Invalid input or violated business rule | CTR-02 missing fields/files, validation failure |
| `403 Forbidden` | Access denied | User lacks required Sales role (AC01) |
| `404 Not Found` | Resource does not exist | Requested ID not found |
| `409 Conflict` | Business state conflict | CTR-01 editing non-draft contract, CTR-03 direct Draft $\rightarrow$ Approved, AC02 re-submitting |
| `500 Internal Server Error` | Unexpected failure | Unhandled server exception |

---

## Testing (Week 07)

The automated test suite lives in `tests/QTKD.Tests/` and is cleanly divided into two layers:

1. **Unit Tests (`UnitTests/`)**: Uses **xUnit + Moq** (mocking `IQtkdRepository`) to verify valid and invalid cases for business rules **CTR-01**, **CTR-02**, **CTR-03**, **AC01**, **AC05**, as well as `CustomerService` and `PricingService`.
2. **Integration Tests (`IntegrationTests/`)**: Uses `WebApplicationFactory<Program>` and connects to an **isolated PostgreSQL test database (`QtkdTestDb`)**. Before running tests, `CustomWebApplicationFactory` runs `EnsureDeleted()` + `Migrate()` on `QtkdTestDb` so the development database (`DB_CONNECTION_STRING`) is never modified or polluted.

Each Integration Test verifies **three layers**: HTTP status code, JSON response payload, and persisted PostgreSQL state.

### Run Automated Tests

```bash
# Run all Unit Tests and Integration Tests (31 tests)
dotnet test tests/QTKD.Tests/QTKD.Tests.csproj

# Run with detailed console output and save TRX evidence report
dotnet test tests/QTKD.Tests/QTKD.Tests.csproj --logger "console;verbosity=normal" --logger "trx;LogFileName=week07_test_results.trx" --results-directory docs/test-results
```

### End-to-End Verification Checklist (Clone $\rightarrow$ API $\rightarrow$ Test)

```bash
git clone <your-repo-url>
cd QTKD.Api
# Create .env containing DB_CONNECTION_STRING=Host=localhost;Port=5432;Database=QtkdDb;Username=postgres;Password=<your_password>
dotnet ef database update
dotnet run            # Verify Swagger & Week 06 workflow
dotnet test tests/QTKD.Tests/QTKD.Tests.csproj
```

---

## Deliverables & Documentation

- **Week 06 Report:** [Tuan_06_Bao_Cao.md](docs/Tuan_06_Bao_Cao.md) — API specifications, US03 / CTR-01–03 / AC01–05 traceability, and demo walkthrough.
- **Week 07 Report & Bug Log:** [Tuan_07_Bao_Cao.md](docs/Tuan_07_Bao_Cao.md) — `TC01–TC08` traceability matrix (`31/31 Passed`), CTR unit test breakdown, bugs found & fixed during testing, and known version limitations.
- **Test Execution Evidence (TRX):** [week07_test_results.trx](docs/test-results/week07_test_results.trx).
- **HTTP Requests:** [QTKD.Api.http](QTKD.Api.http) — Runnable HTTP requests covering all scenarios.

### Known Scope & Design Limitations (Week 07 Baseline)

- **Role simulation:** `AC01` authorization currently reads the `X-User-Role` header (`Sales`) rather than full JWT / ASP.NET Identity authentication.
- **Approval simulation:** Submitting a valid contract records `CurrentApprovalId` and `LastIdempotencyKey` on `Contracts` and transitions status to `PendingApproval` without implementing full multi-step Approval / Payment workflows.
- **Concurrency lock:** Per-contract duplicate submit protection (`SemaphoreSlim`) operates in-memory for a single API instance; horizontal scaling requires database-level optimistic concurrency tokens or a distributed lock.

---

## Roadmap

- [x] Week 05: Solution setup, PostgreSQL connection, initial entities, seed data, and query API
- [x] Week 06: Business rules implementation (US03 Submit Contract, CTR-01, CTR-02, CTR-03, AC01–AC05, Customer CRUD, Pricing management)
- [x] Week 07: Automated unit testing (`CTR-01`/`02`/`03`) & integration testing (`TC01–TC08`, `AC01–AC05`, isolated `QtkdTestDb`, bug fixes) with xUnit & Moq

---

## Author

Built and maintained by **<Your Name>** as a personal learning project for the Information Systems program.

## License

This project is intended for educational purposes. Add a license of your choice (e.g., MIT) if you plan to share it publicly.

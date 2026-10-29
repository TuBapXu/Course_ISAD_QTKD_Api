# QTKD.Api

**Contract & Business Pricing Management System** (*Hệ thống Quản lý Hợp đồng & Giá cước kinh doanh*)

A RESTful Web API built with ASP.NET Core to manage customers, contracts, and price lists. This is a solo academic project I built for learning purposes as part of my Information Systems coursework, with a focus on clean layered architecture, centralized error handling, and secure configuration management.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Npgsql-4169E1?logo=postgresql&logoColor=white)
![EF Core](https://img.shields.io/badge/EF%20Core-Code--First-68217A)
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
├── Exceptions/      # GlobalExceptionMiddleware & custom exceptions
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
| `Exceptions/` | Contains `GlobalExceptionMiddleware`, which catches exceptions globally and maps them to proper HTTP status codes. |

---

## Tech Stack

| Technology | Purpose |
|---|---|
| **.NET 10.0 / ASP.NET Core Web API** | Application framework |
| **PostgreSQL + Npgsql** | Relational database and its .NET provider |
| **Entity Framework Core** | ORM, using the Code-First approach with migrations |
| **DotNetEnv** | Loads configuration from a `.env` file so the database password stays out of source control |
| **Swagger / OpenAPI** | Interactive API documentation and testing UI |

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
| `DB_CONNECTION_STRING` | PostgreSQL connection string used by `QtkdDbContext` | `Host=localhost;Port=5432;Database=qtkd_db;Username=postgres;Password=...` |

---

## Error Handling

All unhandled exceptions are processed by `GlobalExceptionMiddleware` and returned with a consistent status code:

| Status Code | Meaning | Typical Cause |
|---|---|---|
| `400 Bad Request` | Invalid input or violated business rule | Validation failure in the service layer |
| `404 Not Found` | Resource does not exist | Requested ID not found |
| `500 Internal Server Error` | Unexpected failure | Unhandled exception |

---

## Roadmap

- [ ] Add unit tests for the service layer
- [ ] Add authentication & authorization
- [ ] Add pagination, filtering, and sorting for list endpoints
- [ ] Containerize with Docker

---

## Author

Built and maintained by **<Your Name>** as a personal learning project for the Information Systems program.

## License

This project is intended for educational purposes. Add a license of your choice (e.g., MIT) if you plan to share it publicly.

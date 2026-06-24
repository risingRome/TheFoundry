# Operations Performance Analysis Dashboard MVP

ASP.NET Core 8 MVC implementation of the Phase 1 Operations Performance Analysis Dashboard.

## Stack

- ASP.NET Core 8 MVC + Razor Pages Identity
- Entity Framework Core 8
- SQL Server
- Bootstrap 5
- Chart.js

Excluded from Phase 1: Airflow, Kafka, Snowflake, Tableau, and cloud infrastructure.

## Run

Install the .NET 8 SDK, update `src/OpsDashboard.Web/appsettings.json`, then run:

```powershell
dotnet restore
dotnet run --project src/OpsDashboard.Web
```

Seed accounts use password `ChangeMe!234`:

- `admin@ops.local` - Admin
- `analyst@ops.local` - Analyst
- `lead@ops.local` - Team Lead
- `agent@ops.local` - Agent
- `viewer@ops.local` - Viewer

## Solution Structure

```text
OpsDashboardMvp/
  src/
    OpsDashboard.Domain/          Entities and enums
    OpsDashboard.Application/     KPI calculations and dashboard DTOs
    OpsDashboard.Infrastructure/  EF Core, SQL Server, Identity, seed data
    OpsDashboard.Web/             MVC controllers, API controllers, Razor views
  database/
    001_initial_schema.sql        SQL Server domain schema
    README.md                     EF migration commands
  docs/
    architecture.md               Schema, endpoints, classes, wireframes
```

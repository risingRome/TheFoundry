# Database Migrations

The MVP is configured for SQL Server via Entity Framework Core.

Run from the repository root when the .NET 8 SDK is installed:

```powershell
dotnet tool restore
dotnet ef migrations add InitialCreate --project src/OpsDashboard.Infrastructure --startup-project src/OpsDashboard.Web --output-dir Migrations
dotnet ef database update --project src/OpsDashboard.Infrastructure --startup-project src/OpsDashboard.Web
dotnet ef migrations script --project src/OpsDashboard.Infrastructure --startup-project src/OpsDashboard.Web --idempotent --output database/001_initial_ef_idempotent.sql
```

`001_initial_schema.sql` contains the hand-authored SQL Server schema for the operations domain tables. ASP.NET Core Identity tables are created by EF Core from `OpsDashboardDbContext`.

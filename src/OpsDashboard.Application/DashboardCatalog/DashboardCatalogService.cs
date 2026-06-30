using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpsDashboard.Application.Abstractions;
using OpsDashboard.Application.Analytics;
using OpsDashboard.Domain.Entities;
using OpsDashboard.Domain.Enums;

namespace OpsDashboard.Application.DashboardCatalog;

public sealed class DashboardCatalogService(IOpsDashboardDbContext db) : IDashboardCatalogService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<DashboardCatalogItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var dashboards = await db.Dashboards
            .AsNoTracking()
            .Include(x => x.Dataset)
            .ThenInclude(x => x!.Domain)
            .OrderBy(x => x.Status)
            .ThenByDescending(x => x.UpdatedDate)
            .ToListAsync(cancellationToken);

        return dashboards.Select(ToDto).ToList();
    }

    public async Task<DashboardCatalogItemDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var dashboard = await db.Dashboards
            .AsNoTracking()
            .Include(x => x.Dataset)
            .ThenInclude(x => x!.Domain)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return dashboard is null ? null : ToDto(dashboard);
    }

    public async Task<int> SaveAsync(SaveDashboardRequest request, CancellationToken cancellationToken = default)
    {
        var datasetExists = await db.Datasets.AnyAsync(x => x.Id == request.DatasetId, cancellationToken);
        if (!datasetExists)
        {
            return 0;
        }

        var dashboard = new Dashboard
        {
            DatasetId = request.DatasetId,
            Name = NormalizeName(request.Name),
            Status = DashboardStatus.Active,
            FilterJson = JsonSerializer.Serialize(request.Filters, JsonOptions),
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        db.Dashboards.Add(dashboard);
        await db.SaveChangesAsync(cancellationToken);
        return dashboard.Id;
    }

    public async Task<bool> RenameAsync(int id, string name, CancellationToken cancellationToken = default)
    {
        var dashboard = await db.Dashboards.FindAsync([id], cancellationToken);
        if (dashboard is null)
        {
            return false;
        }

        dashboard.Name = NormalizeName(name);
        dashboard.UpdatedDate = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> ArchiveAsync(int id, CancellationToken cancellationToken = default)
    {
        return SetStatusAsync(id, DashboardStatus.Archived, cancellationToken);
    }

    public Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default)
    {
        return SetStatusAsync(id, DashboardStatus.Active, cancellationToken);
    }

    private async Task<bool> SetStatusAsync(int id, DashboardStatus status, CancellationToken cancellationToken)
    {
        var dashboard = await db.Dashboards.FindAsync([id], cancellationToken);
        if (dashboard is null)
        {
            return false;
        }

        dashboard.Status = status;
        dashboard.UpdatedDate = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static DashboardCatalogItemDto ToDto(Dashboard dashboard)
    {
        return new DashboardCatalogItemDto(
            dashboard.Id,
            dashboard.DatasetId,
            dashboard.Name,
            dashboard.Dataset?.DatasetName ?? "Unknown dataset",
            dashboard.Dataset?.Domain?.Name ?? "Unassigned",
            dashboard.Status,
            dashboard.CreatedDate,
            dashboard.UpdatedDate,
            ParseFilters(dashboard.FilterJson));
    }

    private static AnalyticsFilterDto ParseFilters(string filterJson)
    {
        if (string.IsNullOrWhiteSpace(filterJson))
        {
            return AnalyticsFilterDto.Empty;
        }

        try
        {
            return JsonSerializer.Deserialize<AnalyticsFilterDto>(filterJson, JsonOptions) ?? AnalyticsFilterDto.Empty;
        }
        catch (JsonException)
        {
            return AnalyticsFilterDto.Empty;
        }
    }

    private static string NormalizeName(string name)
    {
        var normalized = string.IsNullOrWhiteSpace(name) ? "Untitled dashboard" : name.Trim();
        return normalized.Length <= 180 ? normalized : normalized[..180];
    }
}

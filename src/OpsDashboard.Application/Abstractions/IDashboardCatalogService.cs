using OpsDashboard.Application.DashboardCatalog;

namespace OpsDashboard.Application.Abstractions;

public interface IDashboardCatalogService
{
    Task<IReadOnlyList<DashboardCatalogItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DashboardCatalogItemDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> SaveAsync(SaveDashboardRequest request, CancellationToken cancellationToken = default);
    Task<bool> RenameAsync(int id, string name, CancellationToken cancellationToken = default);
    Task<bool> ArchiveAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default);
}

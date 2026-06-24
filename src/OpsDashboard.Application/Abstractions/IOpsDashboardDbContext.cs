using Microsoft.EntityFrameworkCore;
using OpsDashboard.Domain.Entities;

namespace OpsDashboard.Application.Abstractions;

public interface IOpsDashboardDbContext
{
    DbSet<Agent> Agents { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<CaseType> CaseTypes { get; }
    DbSet<DataFreshness> DataFreshness { get; }
    DbSet<OpsDashboard.Domain.Entities.Domain> Domains { get; }
    DbSet<Dataset> Datasets { get; }
    DbSet<DatasetInsight> DatasetInsights { get; }
    DbSet<OperationsCase> OperationsCases { get; }
    DbSet<Team> Teams { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

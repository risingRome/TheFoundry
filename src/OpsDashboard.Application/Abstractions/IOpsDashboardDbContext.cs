using Microsoft.EntityFrameworkCore;
using OpsDashboard.Domain.Entities;

namespace OpsDashboard.Application.Abstractions;

public interface IOpsDashboardDbContext
{
    DbSet<Agent> Agents { get; }
    DbSet<AssistantConversation> AssistantConversations { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<CaseType> CaseTypes { get; }
    DbSet<DataFreshness> DataFreshness { get; }
    DbSet<DataRefreshJob> DataRefreshJobs { get; }
    DbSet<OpsDashboard.Domain.Entities.Domain> Domains { get; }
    DbSet<OpsDashboard.Domain.Entities.Dashboard> Dashboards { get; }
    DbSet<Dataset> Datasets { get; }
    DbSet<DatasetInsight> DatasetInsights { get; }
    DbSet<OperationsCase> OperationsCases { get; }
    DbSet<Team> Teams { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

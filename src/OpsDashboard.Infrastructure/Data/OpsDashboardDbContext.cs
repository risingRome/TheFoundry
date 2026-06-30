using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpsDashboard.Application.Abstractions;
using OpsDashboard.Domain.Entities;
using OpsDashboard.Infrastructure.Identity;

namespace OpsDashboard.Infrastructure.Data;

public sealed class OpsDashboardDbContext(DbContextOptions<OpsDashboardDbContext> options)
    : IdentityDbContext<ApplicationUser>(options), IOpsDashboardDbContext
{
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<AssistantConversation> AssistantConversations => Set<AssistantConversation>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<CaseType> CaseTypes => Set<CaseType>();
    public DbSet<DataFreshness> DataFreshness => Set<DataFreshness>();
    public DbSet<OpsDashboard.Domain.Entities.Domain> Domains => Set<OpsDashboard.Domain.Entities.Domain>();
    public DbSet<OpsDashboard.Domain.Entities.Dashboard> Dashboards => Set<OpsDashboard.Domain.Entities.Dashboard>();
    public DbSet<Dataset> Datasets => Set<Dataset>();
    public DbSet<DatasetInsight> DatasetInsights => Set<DatasetInsight>();
    public DbSet<OperationsCase> OperationsCases => Set<OperationsCase>();
    public DbSet<Team> Teams => Set<Team>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Team>(entity =>
        {
            entity.ToTable("Teams");
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<Agent>(entity =>
        {
            entity.ToTable("Agents");
            entity.Property(x => x.ExternalAgentId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(150).IsRequired();
            entity.HasIndex(x => x.ExternalAgentId).IsUnique();
            entity.HasOne(x => x.Team).WithMany(x => x.Agents).HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CaseType>(entity =>
        {
            entity.ToTable("CaseTypes");
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.SlaThresholdHours).HasPrecision(9, 2);
            entity.HasIndex(x => x.Code).IsUnique();
        });

        builder.Entity<OperationsCase>(entity =>
        {
            entity.ToTable("OperationsCases");
            entity.Property(x => x.SourceCaseId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.HasIndex(x => x.SourceCaseId).IsUnique();
            entity.HasIndex(x => new { x.TeamId, x.CreatedAtUtc });
            entity.HasIndex(x => new { x.AgentId, x.ResolvedAtUtc });
            entity.HasOne(x => x.Team).WithMany(x => x.Cases).HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Agent).WithMany(x => x.Cases).HasForeignKey(x => x.AgentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CaseType).WithMany(x => x.Cases).HasForeignKey(x => x.CaseTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.Property(x => x.EventType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EventPayloadJson).IsRequired();
            entity.Property(x => x.IpAddress).HasMaxLength(64);
            entity.Property(x => x.PreviousHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.RowHash).HasMaxLength(128).IsRequired();
        });

        builder.Entity<DataFreshness>(entity =>
        {
            entity.ToTable("DataFreshness");
            entity.Property(x => x.SourceName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => x.SourceName).IsUnique();
        });

        builder.Entity<OpsDashboard.Domain.Entities.Domain>(entity =>
        {
            entity.ToTable("Domains");
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.CreatedDate).HasColumnType("datetime2");
            entity.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<Dataset>(entity =>
        {
            entity.ToTable("Datasets");
            entity.Property(x => x.DatasetName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.SourceType).HasMaxLength(50).IsRequired();
            entity.Property(x => x.FilePath).HasMaxLength(1024).IsRequired();
            entity.Property(x => x.UploadDate).HasColumnType("datetime2");
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.DatasetQualityScore).HasPrecision(5, 2);
            entity.HasIndex(x => x.DatasetName).IsUnique();
            entity.HasIndex(x => x.DomainId);
            entity.HasOne(x => x.Domain).WithMany(x => x.Datasets).HasForeignKey(x => x.DomainId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<DatasetInsight>(entity =>
        {
            entity.ToTable("DatasetInsights");
            entity.Property(x => x.BusinessDomain).HasMaxLength(80).IsRequired();
            entity.Property(x => x.DetectedMeasuresJson).IsRequired();
            entity.Property(x => x.DetectedDimensionsJson).IsRequired();
            entity.Property(x => x.RecommendedKpisJson).IsRequired();
            entity.Property(x => x.RecommendedChartsJson).IsRequired();
            entity.Property(x => x.RecommendedDashboardLayoutJson).IsRequired();
            entity.Property(x => x.GeneratedAtUtc).HasColumnType("datetime2");
            entity.HasIndex(x => x.DatasetId).IsUnique();
            entity.HasOne(x => x.Dataset).WithOne().HasForeignKey<DatasetInsight>(x => x.DatasetId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OpsDashboard.Domain.Entities.Dashboard>(entity =>
        {
            entity.ToTable("Dashboards");
            entity.Property(x => x.Name).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.FilterJson).IsRequired();
            entity.Property(x => x.CreatedDate).HasColumnType("datetime2");
            entity.Property(x => x.UpdatedDate).HasColumnType("datetime2");
            entity.HasIndex(x => new { x.DatasetId, x.Status });
            entity.HasOne(x => x.Dataset).WithMany().HasForeignKey(x => x.DatasetId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AssistantConversation>(entity =>
        {
            entity.ToTable("AssistantConversations");
            entity.Property(x => x.UserMessage).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.AssistantResponse).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2");
            entity.Property(x => x.UserId).HasMaxLength(450);
            entity.HasIndex(x => new { x.DatasetId, x.CreatedAt });
            entity.HasOne(x => x.Dataset).WithMany().HasForeignKey(x => x.DatasetId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

using OpsDashboard.Domain.Enums;

namespace OpsDashboard.Domain.Entities;

public sealed class OperationsCase
{
    public long Id { get; set; }
    public string SourceCaseId { get; set; } = string.Empty;
    public int TeamId { get; set; }
    public int AgentId { get; set; }
    public int CaseTypeId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public CaseStatus Status { get; set; } = CaseStatus.Open;
    public bool Escalated { get; set; }
    public bool Reopened { get; set; }
    public DateTime IngestedAtUtc { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public Team Team { get; set; } = null!;
    public Agent Agent { get; set; } = null!;
    public CaseType CaseType { get; set; } = null!;
}

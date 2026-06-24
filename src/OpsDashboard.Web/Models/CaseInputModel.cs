namespace OpsDashboard.Web.Models;

public sealed class CaseInputModel
{
    public string SourceCaseId { get; set; } = string.Empty;
    public int TeamId { get; set; }
    public int AgentId { get; set; }
    public int CaseTypeId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public bool Escalated { get; set; }
    public bool Reopened { get; set; }
}

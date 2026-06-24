namespace OpsDashboard.Domain.Entities;

public sealed class Agent
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public string ExternalAgentId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? IdentityUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public Team Team { get; set; } = null!;
    public ICollection<OperationsCase> Cases { get; set; } = new List<OperationsCase>();
}

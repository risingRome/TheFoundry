namespace OpsDashboard.Domain.Entities;

public sealed class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Agent> Agents { get; set; } = new List<Agent>();
    public ICollection<OperationsCase> Cases { get; set; } = new List<OperationsCase>();
}

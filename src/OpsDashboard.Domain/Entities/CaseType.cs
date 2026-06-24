namespace OpsDashboard.Domain.Entities;

public sealed class CaseType
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal SlaThresholdHours { get; set; } = 24m;
    public ICollection<OperationsCase> Cases { get; set; } = new List<OperationsCase>();
}

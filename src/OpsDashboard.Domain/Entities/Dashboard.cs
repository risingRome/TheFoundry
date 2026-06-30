using OpsDashboard.Domain.Enums;

namespace OpsDashboard.Domain.Entities;

public sealed class Dashboard
{
    public int Id { get; set; }
    public int DatasetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DashboardStatus Status { get; set; } = DashboardStatus.Active;
    public string FilterJson { get; set; } = "{}";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    public Dataset? Dataset { get; set; }
}

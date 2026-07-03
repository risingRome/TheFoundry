using OpsDashboard.Domain.Enums;

namespace OpsDashboard.Domain.Entities;

public sealed class DataRefreshJob
{
    public int Id { get; set; }
    public int DatasetId { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public DataRefreshStatus Status { get; set; } = DataRefreshStatus.Running;
    public int RecordsProcessed { get; set; }
    public string? TriggeredByUserId { get; set; }
    public string? TriggeredByEmail { get; set; }
    public string? Message { get; set; }
    public Dataset? Dataset { get; set; }
}

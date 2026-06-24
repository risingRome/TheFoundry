namespace OpsDashboard.Domain.Entities;

public sealed class AuditLog
{
    public long Id { get; set; }
    public string? UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventPayloadJson { get; set; } = "{}";
    public string? IpAddress { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string PreviousHash { get; set; } = string.Empty;
    public string RowHash { get; set; } = string.Empty;
}

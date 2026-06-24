namespace OpsDashboard.Domain.Entities;

public sealed class DataFreshness
{
    public int Id { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public DateTime RefreshedAtUtc { get; set; }
    public int RowCount { get; set; }
    public string Status { get; set; } = "Success";
}

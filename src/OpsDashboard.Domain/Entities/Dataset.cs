using OpsDashboard.Domain.Enums;

namespace OpsDashboard.Domain.Entities;

public sealed class Dataset
{
    public int Id { get; set; }
    public int DomainId { get; set; }
    public string DatasetName { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    public DatasetStatus Status { get; set; } = DatasetStatus.Uploaded;
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public int MissingValues { get; set; }
    public int DuplicateRows { get; set; }
    public int NumericColumns { get; set; }
    public int DateColumns { get; set; }
    public int TextColumns { get; set; }
    public decimal DatasetQualityScore { get; set; }
    public Domain? Domain { get; set; }
}

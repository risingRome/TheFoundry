using OpsDashboard.Domain.Enums;

namespace OpsDashboard.Domain.Entities;

public sealed class Domain
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DomainStatus Status { get; set; } = DomainStatus.Active;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public ICollection<Dataset> Datasets { get; set; } = new List<Dataset>();
}

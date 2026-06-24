using System.ComponentModel.DataAnnotations;

namespace OpsDashboard.Web.Models;

public sealed class DomainFormViewModel
{
    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }
}

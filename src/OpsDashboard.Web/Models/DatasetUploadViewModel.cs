using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OpsDashboard.Web.Models;

public sealed class DatasetUploadViewModel
{
    [Required]
    [Display(Name = "Domain")]
    public int? DomainId { get; set; }

    [Required]
    [Display(Name = "Dataset file")]
    public IFormFile? File { get; set; }

    public IReadOnlyList<SelectListItem> Domains { get; set; } = Array.Empty<SelectListItem>();
}

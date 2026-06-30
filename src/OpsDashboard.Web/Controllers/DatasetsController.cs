using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OpsDashboard.Application.Abstractions;
using OpsDashboard.Application.Datasets;
using OpsDashboard.Domain.Enums;
using OpsDashboard.Infrastructure.Identity;
using OpsDashboard.Web.Models;

namespace OpsDashboard.Web.Controllers;

[Authorize(Roles = AppRoles.AdminOrAnalyst)]
public sealed class DatasetsController(
    IDatasetService datasets,
    IDatasetExplorerService explorer,
    IBusinessIntelligenceService intelligence,
    IDomainService domains,
    IWebHostEnvironment environment) : Controller
{
    private static readonly IReadOnlyDictionary<string, string> SupportedExtensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".csv"] = "CSV",
        [".xlsx"] = "Excel"
    };

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new DatasetUploadViewModel
        {
            Domains = await GetActiveDomainOptionsAsync(cancellationToken)
        };

        ViewData["Datasets"] = await datasets.GetAllAsync(cancellationToken);
        return View(model);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var dataset = await datasets.GetByIdAsync(id, cancellationToken);
        return dataset is null ? NotFound() : View(dataset);
    }

    public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
    {
        var preview = await explorer.GetPreviewAsync(id, 100, cancellationToken);
        return preview is null ? NotFound() : View(preview);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Upload(DatasetUploadViewModel model, CancellationToken cancellationToken)
    {
        model.Domains = await GetActiveDomainOptionsAsync(cancellationToken);
        ViewData["Datasets"] = await datasets.GetAllAsync(cancellationToken);

        if (model.File is null || model.File.Length == 0)
        {
            ModelState.AddModelError(nameof(model.File), "Choose a non-empty CSV or Excel file.");
        }

        var extension = model.File is null ? string.Empty : Path.GetExtension(model.File.FileName);
        var sourceType = string.Empty;
        if (SupportedExtensions.TryGetValue(extension, out var detectedSourceType))
        {
            sourceType = detectedSourceType;
        }
        else
        {
            ModelState.AddModelError(nameof(model.File), "Only .csv and .xlsx files are supported in v1.3.");
        }

        var datasetName = model.File is null ? string.Empty : Path.GetFileName(model.File.FileName);
        if (!string.IsNullOrWhiteSpace(datasetName) && await datasets.DatasetNameExistsAsync(datasetName, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.File), "A dataset with this file name already exists.");
        }

        if (!ModelState.IsValid || model.DomainId is null || model.File is null)
        {
            return View("Index", model);
        }

        var uploadPath = BuildUploadPath(model.DomainId.Value, datasetName);
        Directory.CreateDirectory(Path.GetDirectoryName(uploadPath)!);
        await using (var stream = System.IO.File.Create(uploadPath))
        {
            await model.File.CopyToAsync(stream, cancellationToken);
        }

        try
        {
            var id = await datasets.RegisterUploadAsync(
                new DatasetUploadRequest(model.DomainId.Value, datasetName, sourceType, uploadPath),
                cancellationToken);
            await intelligence.RefreshInsightAsync(id, cancellationToken);
            TempData["StatusMessage"] = "Dataset uploaded and profiled successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch
        {
            System.IO.File.Delete(uploadPath);
            throw;
        }
    }

    private async Task<IReadOnlyList<SelectListItem>> GetActiveDomainOptionsAsync(CancellationToken cancellationToken)
    {
        var allDomains = await domains.GetAllAsync(cancellationToken);
        return allDomains
            .Where(x => x.Status == DomainStatus.Active)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();
    }

    private string BuildUploadPath(int domainId, string datasetName)
    {
        var safeFileName = string.Join("_", datasetName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var root = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        return Path.Combine(root, "uploads", "datasets", DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"), domainId.ToString(), $"{Guid.NewGuid():N}_{safeFileName}");
    }
}

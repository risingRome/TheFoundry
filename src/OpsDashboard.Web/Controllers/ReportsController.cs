using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsDashboard.Application.Abstractions;
using OpsDashboard.Application.Analytics;
using OpsDashboard.Infrastructure.Identity;
using OpsDashboard.Web.Services;

namespace OpsDashboard.Web.Controllers;

[Authorize(Roles = AppRoles.AdminAnalystExecutive)]
public sealed class ReportsController(IExecutiveReportService reports, IReportExportService exports) : Controller
{
    [HttpGet("/Reports")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await reports.GetReportDatasetsAsync(cancellationToken));
    }

    [HttpGet("/Reports/{datasetId:int}")]
    public async Task<IActionResult> Details(
        int datasetId,
        DateOnly? from,
        DateOnly? to,
        string? department,
        string? region,
        string? employee,
        string? product,
        string? category,
        CancellationToken cancellationToken)
    {
        var report = await reports.GenerateReportAsync(
            datasetId,
            new AnalyticsFilterDto(from, to, department, region, employee, product, category),
            cancellationToken);
        return report is null ? NotFound() : View(report);
    }

    [HttpGet("/Reports/{datasetId:int}/pdf")]
    public async Task<IActionResult> Pdf(
        int datasetId,
        DateOnly? from,
        DateOnly? to,
        string? department,
        string? region,
        string? employee,
        string? product,
        string? category,
        CancellationToken cancellationToken)
    {
        var report = await reports.GenerateReportAsync(
            datasetId,
            new AnalyticsFilterDto(from, to, department, region, employee, product, category),
            cancellationToken);
        if (report is null)
        {
            return NotFound();
        }

        return File(exports.ExportPdf(report), "application/pdf", $"{SafeName(report.DatasetName)}-executive-report.pdf");
    }

    [HttpGet("/Reports/{datasetId:int}/docx")]
    public async Task<IActionResult> Docx(
        int datasetId,
        DateOnly? from,
        DateOnly? to,
        string? department,
        string? region,
        string? employee,
        string? product,
        string? category,
        CancellationToken cancellationToken)
    {
        var report = await reports.GenerateReportAsync(
            datasetId,
            new AnalyticsFilterDto(from, to, department, region, employee, product, category),
            cancellationToken);
        if (report is null)
        {
            return NotFound();
        }

        return File(exports.ExportDocx(report), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"{SafeName(report.DatasetName)}-executive-report.docx");
    }

    private static string SafeName(string value)
    {
        return string.Join("-", value.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
    }
}

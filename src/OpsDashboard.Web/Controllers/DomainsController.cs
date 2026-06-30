using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsDashboard.Application.Abstractions;
using OpsDashboard.Application.Domains;
using OpsDashboard.Infrastructure.Identity;
using OpsDashboard.Web.Models;

namespace OpsDashboard.Web.Controllers;

[Authorize(Roles = AppRoles.AdminOrAnalyst)]
public sealed class DomainsController(IDomainService domains) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await domains.GetAllAsync(cancellationToken));
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var domain = await domains.GetByIdAsync(id, cancellationToken);
        return domain is null ? NotFound() : View(domain);
    }

    [Authorize(Roles = AppRoles.Admin)]
    public IActionResult Create()
    {
        return View(new DomainFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create(DomainFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var id = await domains.CreateAsync(new DomainFormDto(model.Name, model.Description), cancellationToken);
        TempData["StatusMessage"] = "Domain created successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var domain = await domains.GetByIdAsync(id, cancellationToken);
        if (domain is null)
        {
            return NotFound();
        }

        return View(new DomainFormViewModel { Name = domain.Name, Description = domain.Description });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(int id, DomainFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var updated = await domains.UpdateAsync(id, new DomainFormDto(model.Name, model.Description), cancellationToken);
        if (!updated)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Domain updated successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Archive(int id, CancellationToken cancellationToken)
    {
        var archived = await domains.ArchiveAsync(id, cancellationToken);
        if (!archived)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Domain archived.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
    {
        var restored = await domains.RestoreAsync(id, cancellationToken);
        if (!restored)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Domain restored.";
        return RedirectToAction(nameof(Index));
    }
}

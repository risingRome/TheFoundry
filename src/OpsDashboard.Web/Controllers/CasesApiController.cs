using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpsDashboard.Domain.Entities;
using OpsDashboard.Domain.Enums;
using OpsDashboard.Infrastructure.Data;
using OpsDashboard.Infrastructure.Identity;
using OpsDashboard.Web.Models;

namespace OpsDashboard.Web.Controllers;

[ApiController]
[Route("api/v1/cases")]
[Authorize(Roles = AppRoles.AdminOrAnalyst)]
public sealed class CasesApiController(OpsDashboardDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Upsert(CaseInputModel input, CancellationToken cancellationToken)
    {
        var caseType = await db.CaseTypes.FindAsync([input.CaseTypeId], cancellationToken);
        if (caseType is null) return BadRequest("Unknown case type.");

        var existing = await db.OperationsCases.SingleOrDefaultAsync(x => x.SourceCaseId == input.SourceCaseId, cancellationToken);
        var entity = existing ?? new OperationsCase { SourceCaseId = input.SourceCaseId, IngestedAtUtc = DateTime.UtcNow };
        entity.TeamId = input.TeamId;
        entity.AgentId = input.AgentId;
        entity.CaseTypeId = input.CaseTypeId;
        entity.CreatedAtUtc = input.CreatedAtUtc;
        entity.ResolvedAtUtc = input.ResolvedAtUtc;
        entity.Escalated = input.Escalated;
        entity.Reopened = input.Reopened;
        entity.Status = input.ResolvedAtUtc.HasValue ? CaseStatus.Resolved : CaseStatus.Open;

        if (existing is null) db.OperationsCases.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        var tatHours = input.ResolvedAtUtc.HasValue ? Math.Round((input.ResolvedAtUtc.Value - input.CreatedAtUtc).TotalHours, 2) : (double?)null;
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, new
        {
            entity.Id,
            entity.SourceCaseId,
            TatHours = tatHours,
            SlaBreached = tatHours.HasValue && (decimal)tatHours.Value > caseType.SlaThresholdHours
        });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken cancellationToken)
    {
        var item = await db.OperationsCases.AsNoTracking().Include(x => x.CaseType).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }
}

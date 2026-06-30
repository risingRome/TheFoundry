using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpsDashboard.Domain.Entities;
using OpsDashboard.Domain.Enums;
using OpsDashboard.Infrastructure.Identity;

namespace OpsDashboard.Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeAsync(OpsDashboardDbContext db, UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles)
    {
        await db.Database.MigrateAsync();

        foreach (var role in AppRoles.All)
        {
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole(role));
        }

        if (!await db.Teams.AnyAsync())
        {
            db.Teams.AddRange(new Team { Name = "Alpha" }, new Team { Name = "Beta" }, new Team { Name = "Gamma" });
            await db.SaveChangesAsync();
        }

        if (!await db.CaseTypes.AnyAsync())
        {
            db.CaseTypes.AddRange(
                new CaseType { Code = "STANDARD", Name = "Standard Request", SlaThresholdHours = 24m },
                new CaseType { Code = "PRIORITY", Name = "Priority Request", SlaThresholdHours = 12m },
                new CaseType { Code = "COMPLEX", Name = "Complex Investigation", SlaThresholdHours = 36m });
            await db.SaveChangesAsync();
        }

        if (!await db.Agents.AnyAsync())
        {
            var teams = await db.Teams.ToListAsync();
            db.Agents.AddRange(
                new Agent { TeamId = teams.Single(t => t.Name == "Alpha").Id, ExternalAgentId = "A-1001", DisplayName = "Avery Shah" },
                new Agent { TeamId = teams.Single(t => t.Name == "Beta").Id, ExternalAgentId = "B-1001", DisplayName = "Blake Mehta" },
                new Agent { TeamId = teams.Single(t => t.Name == "Gamma").Id, ExternalAgentId = "G-1001", DisplayName = "Casey Rao" });
            await db.SaveChangesAsync();
        }

        await EnsureUser(users, "admin@foundry.local", "Foundry Admin", AppRoles.Admin);
        await EnsureUser(users, "analyst@foundry.local", "Foundry Analyst", AppRoles.Analyst);
        await EnsureUser(users, "executive@foundry.local", "Foundry Executive", AppRoles.Executive);
        await EnsureUser(users, "viewer@foundry.local", "Foundry Viewer", AppRoles.Viewer);

        if (!await db.OperationsCases.AnyAsync())
        {
            var type = await db.CaseTypes.SingleAsync(x => x.Code == "STANDARD");
            var agents = await db.Agents.Include(x => x.Team).ToListAsync();
            var now = DateTime.UtcNow;
            var seedCases = new List<OperationsCase>();
            for (var i = 0; i < 90; i++)
            {
                var agent = agents[i % agents.Count];
                var created = now.Date.AddDays(-i % 30).AddHours(8 + i % 8);
                DateTime? resolved = i % 7 == 0 ? null : created.AddHours(10 + i % 26);
                seedCases.Add(new OperationsCase
                {
                    SourceCaseId = $"CASE-{10000 + i}",
                    AgentId = agent.Id,
                    TeamId = agent.TeamId,
                    CaseTypeId = type.Id,
                    CreatedAtUtc = created,
                    ResolvedAtUtc = resolved,
                    Status = resolved.HasValue ? CaseStatus.Resolved : CaseStatus.Open,
                    Escalated = i % 5 == 0,
                    Reopened = i % 13 == 0
                });
            }
            db.OperationsCases.AddRange(seedCases);
            db.DataFreshness.Add(new DataFreshness { SourceName = "SeedData", RefreshedAtUtc = now, RowCount = seedCases.Count, Status = "Success" });
            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureUser(UserManager<ApplicationUser> users, string email, string name, string role, int? teamId = null, int? agentId = null)
    {
        var existing = await users.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!await users.IsInRoleAsync(existing, role))
            {
                await users.AddToRoleAsync(existing, role);
            }

            return;
        }

        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = name, TeamId = teamId, AgentId = agentId };
        var result = await users.CreateAsync(user, "Foundry!234");
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        await users.AddToRoleAsync(user, role);
    }
}

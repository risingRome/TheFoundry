using Microsoft.AspNetCore.Identity;

namespace OpsDashboard.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public int? TeamId { get; set; }
    public int? AgentId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

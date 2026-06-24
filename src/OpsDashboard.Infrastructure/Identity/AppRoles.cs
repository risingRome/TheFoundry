namespace OpsDashboard.Infrastructure.Identity;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Analyst = "Analyst";
    public const string TeamLead = "Team Lead";
    public const string Agent = "Agent";
    public const string Viewer = "Viewer";

    public static readonly string[] All = [Admin, Analyst, TeamLead, Agent, Viewer];
}

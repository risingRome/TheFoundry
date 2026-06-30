namespace OpsDashboard.Infrastructure.Identity;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Analyst = "Analyst";
    public const string Executive = "Executive";
    public const string Viewer = "Viewer";

    public const string AdminOrAnalyst = Admin + "," + Analyst;
    public const string AdminAnalystExecutive = Admin + "," + Analyst + "," + Executive;
    public const string DashboardLibraryRead = Admin + "," + Analyst + "," + Executive + "," + Viewer;

    public static readonly string[] All = [Admin, Analyst, Executive, Viewer];
}

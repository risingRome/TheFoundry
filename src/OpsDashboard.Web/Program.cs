using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpsDashboard.Infrastructure;
using OpsDashboard.Infrastructure.Data;
using OpsDashboard.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<OpsDashboard.Web.Services.IReportExportService, OpsDashboard.Web.Services.ReportExportService>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(AppRoles.Admin));
    options.AddPolicy("AnalystAccess", policy => policy.RequireRole(AppRoles.Admin, AppRoles.Analyst));
    options.AddPolicy("ExecutiveAccess", policy => policy.RequireRole(AppRoles.Admin, AppRoles.Analyst, AppRoles.Executive));
    options.AddPolicy("DashboardLibraryRead", policy => policy.RequireRole(AppRoles.Admin, AppRoles.Analyst, AppRoles.Executive, AppRoles.Viewer));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OpsDashboardDbContext>();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await SeedData.InitializeAsync(db, users, roles);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute("domains", "Domains", new { controller = "Domains", action = "Index" });
app.MapControllerRoute("datasets", "Datasets", new { controller = "Datasets", action = "Index" });
app.MapControllerRoute("insights", "Insights", new { controller = "Insights", action = "Index" });
app.MapControllerRoute("reports", "Reports", new { controller = "Reports", action = "Index" });
app.MapControllerRoute("dashboard-library", "DashboardLibrary", new { controller = "DashboardLibrary", action = "Index" });
app.MapControllerRoute("default", "{controller=Dashboard}/{action=Executive}/{id?}");
app.MapRazorPages();

app.Run();

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
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ExecutiveAccess", policy => policy.RequireRole(AppRoles.Admin, AppRoles.Analyst));
    options.AddPolicy("TeamAccess", policy => policy.RequireRole(AppRoles.Admin, AppRoles.Analyst, AppRoles.TeamLead, AppRoles.Viewer));
    options.AddPolicy("AgentAccess", policy => policy.RequireRole(AppRoles.Admin, AppRoles.Analyst, AppRoles.TeamLead, AppRoles.Agent));
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
app.MapControllerRoute("default", "{controller=Dashboard}/{action=Executive}/{id?}");
app.MapRazorPages();

app.Run();

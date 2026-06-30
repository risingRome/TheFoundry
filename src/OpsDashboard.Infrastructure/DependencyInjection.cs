using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpsDashboard.Application.Abstractions;
using OpsDashboard.Application.Assistant;
using OpsDashboard.Application.Datasets;
using OpsDashboard.Application.DashboardCatalog;
using OpsDashboard.Application.Dashboards;
using OpsDashboard.Application.Domains;
using OpsDashboard.Application.GeneratedDashboards;
using OpsDashboard.Application.Insights;
using OpsDashboard.Application.Reports;
using OpsDashboard.Infrastructure.Data;
using OpsDashboard.Infrastructure.Datasets;
using OpsDashboard.Infrastructure.Identity;

namespace OpsDashboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OpsDashboardDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("OpsDashboard")));

        services.AddScoped<IOpsDashboardDbContext>(sp => sp.GetRequiredService<OpsDashboardDbContext>());
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IDashboardCatalogService, DashboardCatalogService>();
        services.AddScoped<IExecutiveAssistantService, ExecutiveAssistantService>();
        services.AddScoped<IDomainService, DomainService>();
        services.AddScoped<IDatasetService, DatasetService>();
        services.AddScoped<IDatasetExplorerService, DatasetExplorerService>();
        services.AddScoped<IBusinessIntelligenceService, BusinessIntelligenceService>();
        services.AddScoped<IGeneratedDashboardService, GeneratedDashboardService>();
        services.AddScoped<IExecutiveReportService, ExecutiveReportService>();
        services.AddScoped<IDatasetFileProfiler, DatasetFileProfiler>();
        services.AddScoped<IDatasetProfileReader, CsvDatasetProfileReader>();
        services.AddScoped<IDatasetProfileReader, XlsxDatasetProfileReader>();

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequiredLength = 10;
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddEntityFrameworkStores<OpsDashboardDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }
}

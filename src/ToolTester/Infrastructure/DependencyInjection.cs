using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Infrastructure.Persistance;
using ToolTester.Infrastructure.Services;

namespace ToolTester.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {


        var dbPath = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      "tooltester.db");

        services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite($"Data Source={dbPath}"));

        services.AddDbContextFactory<ApplicationDbContext>(options =>
        options.UseSqlite($"Data Source={dbPath}"));

        services.AddScoped<IApplicationDbContext>(
        provider => provider.GetRequiredService<ApplicationDbContext>());


        services.AddScoped<IApplicationDbContext>(provider => provider.GetService<ApplicationDbContext>());
        services.AddScoped<IApplicationDbContextSeed, ApplicationDbContextSeed>();
        services.AddTransient<IDateTime, DateTimeService>();
        services.AddTransient<IExcelService, ExcelService>();
        services.AddTransient<IUploadService, UploadService>();
        services.AddScoped<IParsingService, ParsingService>();
        services.AddScoped<IReportingService,ReportingService>();
        services.AddScoped<IZipfileService,ZipfileService>();
        services.AddScoped<ICweRelationshipService, CweRelationshipService>();
        services.AddScoped<ISemanticRuleProvider, JsonSemanticRuleProvider>();
        services.AddScoped <ICweTopologyService,CweTopologyService > ();
        services.AddScoped<ICweRootCauseResolver, CweRootCauseResolver>();
        services.AddScoped<BenchmarkReportService>();
        return services;
    }


}

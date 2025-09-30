using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Domain.Coomon.Interfaces;
using ToolTester.Infrastructure.Persistance;
using ToolTester.Infrastructure.Services;

namespace ToolTester.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        

        services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("ToolTester")

                );
        services.AddDbContextFactory<ApplicationDbContext>(
     options =>
       options.UseInMemoryDatabase("ToolTester"));


        services.AddScoped<IApplicationDbContext>(provider => provider.GetService<ApplicationDbContext>());
        services.AddTransient<IDateTime, DateTimeService>();
        services.AddTransient<IExcelService, ExcelService>();
        services.AddTransient<IUploadService, UploadService>();
        services.AddScoped<IParsingService, ParsingService>();
        services.AddScoped<IReportingService,ReportingService>();

        return services;
    }


}

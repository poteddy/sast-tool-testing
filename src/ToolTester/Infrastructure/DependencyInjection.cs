using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Infrastructure.Persistance;
using ToolTester.Infrastructure.Services;

namespace ToolTester.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {

        var connection = new SqliteConnection("Data Source=data1.db");

        services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(connection,
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))

                );
      
         services.AddScoped<IApplicationDbContext>(provider => provider.GetService<ApplicationDbContext>());     
        services.AddTransient<IDateTime, DateTimeService>();
        services.AddTransient<IExcelService, ExcelService>();
        services.AddTransient<IUploadService, UploadService>();
           

        return services;
    }

 
}

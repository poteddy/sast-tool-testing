using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Infrastructure.Persistance;

namespace ToolTester.ConsoleApp.Menus
{
    public class View
    {
        private readonly ILogger<Program> _logger;

        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private IServiceProvider _serviceProvider;

        public View(ILogger<Program> logger, IDbContextFactory<ApplicationDbContext> contextFactory, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _serviceProvider = serviceProvider;
        }
        public async Task Display()
        {
            var reportservice = _serviceProvider.GetRequiredService<IReportingService>();
            var result = await reportservice.GenerateReport(1);
        }
    }
}

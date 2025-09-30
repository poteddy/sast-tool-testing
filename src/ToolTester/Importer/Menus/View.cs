using Importer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolTester.Domain.Coomon.Interfaces;
using ToolTester.Domain.Entities;
using ToolTester.Infrastructure;
using ToolTester.Infrastructure.Persistance;
using ToolTester.Parsers.Sarif.Interfaces;

namespace ToolTester.Importer.Menus
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

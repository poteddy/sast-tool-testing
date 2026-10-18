using Microsoft.EntityFrameworkCore;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Infrastructure.Persistance;

namespace ToolTester.Infrastructure.Services
{
    public class ScanDisplayNameProvider : IScanDisplayNameProvider
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public ScanDisplayNameProvider(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        public string GetDisplayName(int scanId)
        {
            using var ctx = _contextFactory.CreateDbContext();
            var scan = ctx.Scans.Find(scanId);
            return string.IsNullOrWhiteSpace(scan?.Name) ? $"Scan {scanId}" : scan!.Name;
        }
    }
}
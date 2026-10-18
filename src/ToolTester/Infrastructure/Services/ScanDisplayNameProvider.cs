using Microsoft.EntityFrameworkCore;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Infrastructure.Persistance;

namespace ToolTester.Infrastructure.Services
{
    public class ScanDisplayNameProvider : IScanDisplayNameProvider
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private IReadOnlyDictionary<int, string> _scanNames = new Dictionary<int, string>();

        public ScanDisplayNameProvider(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            _scanNames = await context.Scans
                .AsNoTracking()
                .ToDictionaryAsync(
                    scan => scan.Id,
                    scan => string.IsNullOrWhiteSpace(scan.Name) ? $"Scan {scan.Id}" : scan.Name,
                    cancellationToken);
        }

        public string GetScanName(int scanId) =>
            _scanNames.TryGetValue(scanId, out var scanName) ? scanName : $"Scan {scanId}";

        public string GetScanDisplayName(int scanId) =>
            $"{GetScanName(scanId)} ({scanId})";
    }
}
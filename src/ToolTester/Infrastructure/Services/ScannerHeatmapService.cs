using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Reports;
using ToolTester.Application.Reports.DTO;

namespace ToolTester.Infrastructure.Services
{
    // Infrastructure implementation: aggregates benchmark rows into a HeatmapDto.
    // Replace ScannerFalseNegativeDto with your existing DTO returned by BenchmarkReportService.
    public class ScannerHeatmapService : IHeatmapQuery
    {
        private readonly BenchmarkReportService _benchmarkService;
        private readonly IScanDisplayNameProvider _scanDisplayNameProvider;
        private readonly Func<int, string> _getScanDisplayName;

        public ScannerHeatmapService(
     BenchmarkReportService benchmarkService,
     IScanDisplayNameProvider scanDisplayNameProvider)
        {
            _benchmarkService = benchmarkService ?? throw new ArgumentNullException(nameof(benchmarkService));
            _scanDisplayNameProvider = scanDisplayNameProvider ?? throw new ArgumentNullException(nameof(scanDisplayNameProvider));
        }
        public async Task<HeatmapDto> GetHeatmapAsync(CancellationToken cancellationToken = default)
        {
            // Acquire the raw rows from existing benchmark service (async)
            var rows = await _benchmarkService.GetFalseNegativesByScannerAsync();

            // Filter and project to DTOs (keep aggregation logic, but return application DTOs)
            var source = rows.Where(r => r.Opportunities > 0).ToList();

            var dto = new HeatmapDto();
            if (source.Count == 0) return dto;

            var scans = source
                .Select(item => item.ScanId)
                .Distinct()
                .Select(scanId => new ScanInfoDto
                {
                    ScanId = scanId,
                    ScanName = _scanDisplayNameProvider.GetScanDisplayName(scanId)
                })
                .OrderBy(s => s.ScanName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(s => s.ScanId)
                .ToList();

            var cwes = source
                .GroupBy(item => item.CweId)
                .Select(g => new CweInfoDto
                {
                    CweId = g.Key,
                    CweName = g.Select(x => x.CweName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? $"CWE-{g.Key}"
                })
                .OrderBy(c => c.CweId)
                .ToList();

            var scanIndexById = scans.Select((s, i) => new { s.ScanId, Index = i }).ToDictionary(x => x.ScanId, x => x.Index);

            var cells = source
                .GroupBy(item => new { item.ScanId, item.CweId })
                .Select(group =>
                {
                    var opportunities = group.Sum(x => x.Opportunities);
                    var detected = group.Sum(x => x.Detected);
                    var falseNegatives = group.Sum(x => x.FalseNegatives);
                    var detectionRate = opportunities <= 0 ? 0 : detected * 100.0 / opportunities;

                    return new HeatmapCellDto
                    {
                        ScanId = group.Key.ScanId,
                        CweId = group.Key.CweId,
                        CweName = group.Select(x => x.CweName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? $"CWE-{group.Key.CweId}",
                        Opportunities = opportunities,
                        Detected = detected,
                        FalseNegatives = falseNegatives,
                        DetectionRate = Math.Clamp(detectionRate, 0, 100)
                    };
                })
                .OrderBy(c => c.CweId)
                .ThenBy(c => scanIndexById[c.ScanId])
                .ToList();

            dto = new HeatmapDto
            {
                Scans = scans,
                Cwes = cwes,
                Cells = cells
            };

            return dto;
        }
    }
}
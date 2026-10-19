using Microsoft.EntityFrameworkCore;
using ToolTester.Domain.Entities;
using ToolTester.Infrastructure.Persistance;
namespace ToolTester.Infrastructure.Services
{
    public sealed class BenchmarkReportService
    {
        private readonly ApplicationDbContext _context;

        public BenchmarkReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ScannerFalseNegativeDto>>
        GetFalseNegativesByScannerAsync()
        {
            var coverage = await _context.JulietCoverages
                .ToListAsync();

            var reports = await _context.Reports
                .ToListAsync();
            var scans = await _context.Scans
       .AsNoTracking()
       .Where(scan =>
           _context.Reports.Any(report =>
               report.ScanId == scan.Id))
       .Select(scan => new
       {
           ScanId = scan.Id,
           scan.ToolId
       })
       .ToListAsync();
            var results = new List<ScannerFalseNegativeDto>();

            foreach (var scan in scans)
            {
                foreach (var cwe in coverage)
                {
                    var detected = reports
    .Where(r =>
    r.ScanId == scan.ScanId &&
    r.GroundTruthCweId == cwe.CweId &&
    r.Relationship !=
    CweRelationshipKind.Unrelated.ToString()
    &&
    r.Relationship !=
    CweRelationshipKind.DetectedWithoutCwe.ToString())
    .Sum(r => r.Count);
                    results.Add(
                        new ScannerFalseNegativeDto
                        {
                            ScanId = scan.ScanId,

                            CweId = cwe.CweId,

                            CweName =

                                $"CWE-{cwe.CweId}",

                            Opportunities =
                                cwe.Covered,

                            Detected =
                                detected,

                            FalseNegatives =
                                Math.Max(
                                    0,
                                    cwe.Covered - detected)
                        });
                }
            }

            return results;
        }

        public async Task<List<ScannerFalseNegativeDto>>
      GetFalseNegativesByScannerDeclaredAsync()
        {
            var coverage = await _context.JulietCoverages
                .AsNoTracking()
                .ToListAsync();

            var reports = await _context.Reports
                .AsNoTracking()
                .ToListAsync();

            /*
             * Scan owns the authoritative ToolId.
             *
             * Do not derive the tool association from Report.ToolId.
             */
            var scans = await _context.Scans
                .AsNoTracking()
                .Where(scan =>
                    _context.Reports.Any(report =>
                        report.ScanId == scan.Id))
                .Select(scan => new
                {
                    ScanId = scan.Id,
                    scan.ToolId
                })
                .ToListAsync();

            /*
             * Load declared coverage once instead of querying
             * ToolDeclaredCwes once per scan.
             */
            var declaredCoverage = await _context.ToolDeclaredCwes
                .AsNoTracking()
                .Select(item => new
                {
                    item.ToolId,
                    item.CweId
                })
                .ToListAsync();

            var declaredByTool = declaredCoverage
                .GroupBy(item => item.ToolId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(item => item.CweId)
                        .ToHashSet());

            var results =
                new List<ScannerFalseNegativeDto>();

            foreach (var scan in scans)
            {
                /*
                 * This is intentionally NOT a fallback to all Juliet
                 * coverage.
                 *
                 * This method represents declared coverage. If the tool
                 * has no declarations configured, it should not silently
                 * be treated as declaring every Juliet CWE.
                 */
                if (!declaredByTool.TryGetValue(
                        scan.ToolId,
                        out var declaredCweIds) ||
                    declaredCweIds.Count == 0)
                {
                    continue;
                }

                var coverageForScan = coverage
                    .Where(cwe =>
                        declaredCweIds.Contains(cwe.CweId));

                foreach (var cwe in coverageForScan)
                {
                    var detected = reports
                        .Where(report =>
                            report.ScanId == scan.ScanId &&
                            report.GroundTruthCweId ==
                                cwe.CweId &&
                            report.Relationship !=
                                CweRelationshipKind.Unrelated
                                    .ToString() &&
                            report.Relationship !=
                                CweRelationshipKind
                                    .DetectedWithoutCwe
                                    .ToString())
                        .Sum(report =>
                            report.Count);

                    results.Add(
                        new ScannerFalseNegativeDto
                        {
                            ScanId =
                                scan.ScanId,

                            CweId =
                                cwe.CweId,

                            CweName =
                                $"CWE-{cwe.CweId}",

                            Opportunities =
                                cwe.Covered,

                            Detected =
                                detected,

                            FalseNegatives =
                                Math.Max(
                                    0,
                                    cwe.Covered - detected)
                        });
                }
            }

            return results;
        }
    }
        public sealed class ScannerFalseNegativeDto
    {
        public int ScanId { get; set; }

        public int CweId { get; set; }

        public string CweName { get; set; } = string.Empty;

        public int Opportunities { get; set; }

        public int Detected { get; set; }

        public int FalseNegatives { get; set; }
    }
}

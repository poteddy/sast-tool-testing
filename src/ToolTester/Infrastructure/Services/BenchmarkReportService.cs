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
            var scans = await _context.Reports
.Select(r => new
{
    r.ScanId,
    r.ToolId
})
.Distinct()
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
            // load the global Juliet coverage once
            var coverage = await _context.JulietCoverages
                .ToListAsync();

            var reports = await _context.Reports
                .ToListAsync();

            var scans = await _context.Reports
                .Select(r => new
                {
                    r.ScanId,
                    r.ToolId
                })
                .Distinct()
                .ToListAsync();

            var results = new List<ScannerFalseNegativeDto>();

            foreach (var scan in scans)
            {
                // load declared CWEs for this tool (scanner). If none declared, fall back to global coverage.
                var declaredCweIds = await _context.ToolDeclaredCwes
                    .Where(td => td.ToolId == scan.ToolId)
                    .Select(td => td.CweId)
                    .ToListAsync();

                IEnumerable<JulietCoverage> coverageForScan;
                if (declaredCweIds != null && declaredCweIds.Count > 0)
                {
                    // filter JulietCoverage to only those CWEs the scanner declares it covers
                    var declaredSet = declaredCweIds.ToHashSet();
                    coverageForScan = coverage.Where(c => declaredSet.Contains(c.CweId));
                }
                else
                {
                    // no declared coverage: use full Juliet coverage
                    coverageForScan = coverage;
                }

                foreach (var cwe in coverageForScan)
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

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
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
    CweRelationshipKind.Unrelated.ToString())
    .Sum(r => r.Count);
                    results.Add(
                        new ScannerFalseNegativeDto
                        {
                            ScanId = scan.ScanId,

                            CweId = cwe.CweId,

                            CweName =
                                cwe.CWECatalog?.Name ??
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

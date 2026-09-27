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

            var tools = await _context.Tools
                .Select(x => new
                {
                    x.Id,
                    x.Name
                })
                .ToListAsync();

            var reports = await _context.Reports
                .ToListAsync();

            var results = new List<ScannerFalseNegativeDto>();

            foreach (var tool in tools)
            {
                foreach (var cwe in coverage)
                {
                    var detected = reports
    .Where(r =>
    r.ToolId == tool.Id &&
    r.GroundTruthCweId == cwe.CweId &&
    r.Relationship !=
    CweRelationshipKind.Unrelated.ToString())
    .Sum(r => r.Count);
                    results.Add(
                        new ScannerFalseNegativeDto
                        {
                            ScannerName = tool.Name,

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
        public string ScannerName { get; set; } = string.Empty;

        public int CweId { get; set; }

        public string CweName { get; set; } = string.Empty;

        public int Opportunities { get; set; }

        public int Detected { get; set; }

        public int FalseNegatives { get; set; }
    }
}

using ToolTester.Domain;
using Microsoft.EntityFrameworkCore;
using ToolTester.Domain.Entities;

namespace ToolTester.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Domain.Entities.CWECatalog> CWECatalogs { get; set; }
        DbSet<Relationship> Relationships { get; set; }
        DbSet<CWETestResult>  CWETestResults  { get; set; }
        DbSet<JulietCoverage>  JulietCoverages  { get; set; }
        DbSet<Report> Reports { get; set; }
        DbSet<Scan> Scans { get; set; }
        DbSet<Tool> Tools { get; set; }
    }
}
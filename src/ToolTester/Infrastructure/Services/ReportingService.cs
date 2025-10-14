using Microsoft.CodeAnalysis.Sarif;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using ToolTester.Domain.Coomon.Interfaces;
using ToolTester.Domain.Entities;
using ToolTester.Infrastructure.Persistance;
using ToolTester.Parsers.Sarif.Interfaces;

namespace ToolTester.Infrastructure.Services
{
    public class ReportingService : IReportingService
    {
        private readonly ILogger<ReportingService> _logger;

        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private bool disposedValue;

        public ReportingService(ILogger<ReportingService> logger, IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _logger = logger;
            _contextFactory = contextFactory;
        }
        public async Task<StringBuilder> GenerateReport(int scanid,int toolid)
        {
            using (var context = this._contextFactory.CreateDbContext())
            {
                try
                {
                    var Catalogs = context.CWECatalogs.AsNoTracking().OrderBy(d => d.CweId).ToList();
                    List<Domain.Entities.Relationship> relations = context.Relationships.AsNoTracking().ToList();

                    List<Domain.Entities.CWETestResultBase> testResults = context.CWETestResults.AsNoTracking().Select(d => new CWETestResultBase() { ScannerFoundCWE = d.ScannerFoundCWE, TestPathListedCWE = d.TestPathListedCWE, Test = d.Test }).ToList();
                    foreach (var c in Catalogs)
                    {
                      
                        var cwes = testResults.Count(d => d.TestPathListedCWE == c.CweId && d.ScannerFoundCWE == c.CweId);
                        if (cwes > 0)
                        {
                            var thirereport = new Report()
                            {
                                CweId = c.CweId,
                                RelatedId = c.CweId,
                                Count = cwes,
                                ScanId = scanid,
                                ToolId = toolid,
                                Relationship = RelatedNatureEnumeration.PeerOf.ToString()
                            };

                            context.Reports.Add(thirereport);
                            await context.SaveChangesAsync();
                            _logger.LogInformation($"Test {c.CweId} has {cwes} exact matches");
                        }
                        GetRelation(testResults, relations, RelatedNatureEnumeration.PeerOf, c.CweId,scanid,toolid);

                        var firstgenparents = await GetRelation(testResults, relations, RelatedNatureEnumeration.ParentOf, c.CweId, scanid, toolid);
                        // if(firstgenparents.Count() > 0)  _logger.Information($"Test grand parents of {c.Id}");
                        foreach (var i in firstgenparents)
                        {

                            await GetRelation(testResults, relations, RelatedNatureEnumeration.ParentOf, i, scanid, toolid);
                        }
                        var firstgenchildren = await GetRelation(testResults, relations, RelatedNatureEnumeration.ChildOf, c.CweId, scanid, toolid);
                        //  if(firstgenchildren.Count() >0) _logger.Information($"Test grand children of {c.Id}");
                        foreach (var i in firstgenparents)
                        {

                            await GetRelation(testResults, relations, RelatedNatureEnumeration.ChildOf, i, scanid, toolid);
                        }

                    }

                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message, ex);
                    throw;
                }

            }

            StringBuilder report = new StringBuilder();

            return report;
        }
        private async Task<List<int>> GetRelation(List<Domain.Entities.CWETestResultBase> results, List<Domain.Entities.Relationship> relations, RelatedNatureEnumeration relation, int cwe, int scanid, int toolid)
        {
            var thischildrelations = relations.Where(d => d.CweId == cwe && d.Nature == relation.ToString()).ToList();
            foreach (var thisrealtion in thischildrelations)
            {
                var parent = results.Count(d => d.TestPathListedCWE == cwe && d.ScannerFoundCWE == thisrealtion.RelatedCweID);
                if (parent > 0)
                {
                    using (var context = this._contextFactory.CreateDbContext())
                    {
                        var thirereport = new Report()
                        {
                            CweId = cwe,
                            RelatedId = thisrealtion.RelatedCweID,
                            Count = parent,
                            ScanId = scanid,
                            ToolId = toolid,
                            Relationship = relation.ToString()
                        };

                        context.Reports.Add(thirereport);
                        await context.SaveChangesAsync();
                    }
                        _logger.LogInformation($"Test {cwe} has {parent} {relation} matches of {thisrealtion.RelatedCweID}");
                }
            }
            return thischildrelations.Select(d => d.RelatedCweID).ToList();
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ReportingService()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

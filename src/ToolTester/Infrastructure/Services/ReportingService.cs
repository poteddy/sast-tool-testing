using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using ToolTester.Domain.Coomon.Interfaces;
using ToolTester.Domain.Entities;
using ToolTester.Infrastructure.Persistance;

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
        public async Task<StringBuilder> GenerateReport(int testid)
        {
            using (var context = this._contextFactory.CreateDbContext())
            {
                try
                {
                    var Catalogs = context.CWECatalogs.AsNoTracking().OrderBy(d => d.Id).ToList();
                    List<Domain.Entities.Relationship> relations = context.Relationships.AsNoTracking().ToList();

                    List<Domain.Entities.CWETestResultBase> testResults = context.CWETestResults.AsNoTracking().Select(d => new CWETestResultBase() { ScannerFoundCWE = d.ScannerFoundCWE, TestPathListedCWE = d.TestPathListedCWE, Test = d.Test }).ToList();
                    foreach (var c in Catalogs)
                    {
                        var testpath = "D:\\github\\juliet\\testcases";

                        var cwes = testResults.Count(d => d.TestPathListedCWE == c.Id && d.ScannerFoundCWE == c.Id);
                        if (cwes > 0)
                        {
                            _logger.LogInformation($"Test {c.Id} has {cwes} exact matches");
                        }
                        GetRelation(testResults, relations, RelatedNatureEnumeration.PeerOf, c.Id);

                        var firstgenparents = await GetRelation(testResults, relations, RelatedNatureEnumeration.ParentOf, c.Id);
                        // if(firstgenparents.Count() > 0)  _logger.Information($"Test grand parents of {c.Id}");
                        foreach (var i in firstgenparents)
                        {

                            await GetRelation(testResults, relations, RelatedNatureEnumeration.ParentOf, i);
                        }
                        var firstgenchildren = await GetRelation(testResults, relations, RelatedNatureEnumeration.ChildOf, c.Id);
                        //  if(firstgenchildren.Count() >0) _logger.Information($"Test grand children of {c.Id}");
                        foreach (var i in firstgenparents)
                        {

                            await GetRelation(testResults, relations, RelatedNatureEnumeration.ChildOf, i);
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
        private async Task<List<int>> GetRelation(List<Domain.Entities.CWETestResultBase> results, List<Domain.Entities.Relationship> relations, RelatedNatureEnumeration relation, int cwe)
        {
            var thischildrelations = relations.Where(d => d.CWEID == cwe && d.Nature == relation.ToString()).ToList();
            foreach (var thisrealtion in thischildrelations)
            {
                var parent = results.Count(d => d.TestPathListedCWE == cwe && d.ScannerFoundCWE == thisrealtion.RelatedCweID);
                if (parent > 0)
                {
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

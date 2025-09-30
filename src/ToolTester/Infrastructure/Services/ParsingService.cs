using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ToolTester.Domain.Coomon.Interfaces;
using ToolTester.Domain.Entities;
using ToolTester.Infrastructure.Persistance;
using ToolTester.Parsers.Sarif;
using ToolTester.Parsers.Sarif.Interfaces;

namespace ToolTester.Infrastructure.Services
{
    public class ParsingService : IParsingService
    {
        private readonly ILogger<ParsingService> _logger;

        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;


        public ParsingService(ILogger<ParsingService> logger, IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _logger = logger;
            _contextFactory = contextFactory;
        }
        private bool disposedValue;

        public async Task<int> Parse(int ToolId, string filepath)
        {

            if (ToolId == 1) //sarif
            {
                var parser = new ToolTester.Parsers.Sarif.Parser();
                FileStream fs = File.OpenRead(filepath.Replace("\"", ""));
                var cwes = await parser.Get_findings(fs);
                await SaveReport(cwes);
                return cwes.Count;
            }

            else if (ToolId == 2)
            {

            }

            return 0;
        }
        private async Task<bool> SaveReport(List<CWEs> cwes)
        {
            if (cwes.Count > 0)
            {
                foreach (var cweresult in cwes)
                {
                    string pattern = $@"(?<=CWE)\d+";

                    Match match = Regex.Match(cweresult.FilePath, pattern);

                    if (match.Success)
                    {


                        var thisresult = new CWETestResult()
                        {
                            TestPathListedCWE = int.Parse(match.Value),
                            Cve = cweresult.Cve + "",
                            ScannerFoundCWE = cweresult.Cwe,
                            Date = DateTime.Now,
                            Description = cweresult.Description + "",
                            DynamicFinding = cweresult.DynamicFinding,
                            FilePath = cweresult.FilePath + "",
                            FoundBy = cweresult.FoundBy,
                            Line = cweresult.Line,
                            Mitigation = cweresult.Mitigation + "",
                            NumericalSeverity = cweresult.NumericalSeverity,
                            References = cweresult.References + "",
                            Severity = cweresult.Severity + "",
                            StaticFinding = cweresult.StaticFinding,
                            Test = cweresult.Test,
                            Title = cweresult.Title + "",
                            VulnIdFromTool = cweresult.VulnIdFromTool + ""

                        };

                        using (var context = this._contextFactory.CreateDbContext())
                        {
                            try
                            {
                                context.CWETestResults.Add(thisresult);
                                await context.SaveChangesAsync();

                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex.Message, ex);
                                throw;
                            }

                        }

                    }

                }
                return true;

            }
            return false;
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
        // ~ParsingService()
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

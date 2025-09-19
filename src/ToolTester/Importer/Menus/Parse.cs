using Importer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using System.IO;
using System.Text;
using System.Text.Json;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Domain.Entities;
using ToolTester.Infrastructure.Persistance;
using ToolTester.Parsers.Sarif.Interfaces;

namespace ToolTester.Importer.Menus
{
    internal class Parse
    {
        private readonly ILogger<Program> _logger;

        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;


        public Parse(ILogger<Program> logger, IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _logger = logger;
            _contextFactory = contextFactory;
        }

        public async Task<List<CWEs>> Display()
        {
            Console.Write("Type Parser: ");

            var input = Console.ReadLine().ToLower();
            if (input == "Sarif".ToLower())
            {
                ToolTester.Parsers.Sarif.Parser parser = new ToolTester.Parsers.Sarif.Parser();

                Console.Write("input file: ");
                var filepath = Console.In.ReadLine();

                FileStream fs = File.OpenRead(filepath);
                var cwes = parser.get_findings(fs);

                if (cwes.Count() > 0)
                {
                    foreach (var cweresult in cwes)
                    {

                        var thisresult = new CWETestResult()
                        {
                            Cve = cweresult.Cve +"",
                            Cwe = cweresult.Cwe,
                            Date = DateTime.Now,
                            Description = cweresult.Description + "",
                            DynamicFinding = cweresult.DynamicFinding ,
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

                return cwes;

            }

            return null;
        }
    }
}

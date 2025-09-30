using Importer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolTester.Domain.Entities;
using ToolTester.Infrastructure;
using ToolTester.Infrastructure.Persistance;
using ToolTester.Parsers.Sarif.Interfaces;

namespace ToolTester.Importer.Menus
{
    public class View
    {
        private readonly ILogger<Program> _logger;

        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;


        public View(ILogger<Program> logger, IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _logger = logger;
            _contextFactory = contextFactory;
        }
        public async Task Display()
        {
            using (var context = this._contextFactory.CreateDbContext())
            {
                try
                {
                    var Catalogs = context.CWECatalogs.AsNoTracking().OrderBy(d => d.Id).ToList();
                    List<Domain.Entities.Relationssship> relations = context.RelationsShips.AsNoTracking().ToList();
                  
                    List<Domain.Entities.CWETestResultBase> testResults = context.CWETestResults.AsNoTracking().Select(d => new CWETestResultBase(){ Cwe=d.Cwe, PathCWe = d.PathCWe, Test= d.Test  }).ToList();
                    foreach (var c in Catalogs)
                    {                      
                        var testpath = "D:\\github\\juliet\\testcases";
                      
                        var cwes = testResults.Count(d => d.PathCWe == c.Id && d.Cwe == c.Id);
                        if (cwes > 0)
                        {
                            Console.WriteLine($"Test {c.Id} has {cwes} exact matches");
                        }
                        GetRelation(testResults, relations, RelatedNatureEnumeration.PeerOf,  c.Id);

                        var firstgenparents = await GetRelation(testResults, relations, RelatedNatureEnumeration.ParentOf, c.Id);
                       // if(firstgenparents.Count() > 0)  Console.WriteLine($"Test grand parents of {c.Id}");
                        foreach (var i in firstgenparents)
                        {
                        
                            await GetRelation(testResults, relations, RelatedNatureEnumeration.ParentOf,  i);
                        }
                        var firstgenchildren = await GetRelation(testResults, relations, RelatedNatureEnumeration.ChildOf, c.Id);
                      //  if(firstgenchildren.Count() >0) Console.WriteLine($"Test grand children of {c.Id}");
                        foreach (var i in firstgenparents)
                        {
                            
                            await GetRelation(testResults, relations, RelatedNatureEnumeration.ChildOf,  i);
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


        }
        private async Task<List<int>> GetRelation(List<Domain.Entities.CWETestResultBase> results, List<Domain.Entities.Relationssship> relations, RelatedNatureEnumeration relation, int cwe)
        {
            var thischildrelations = relations.Where(d => d.CWEID == cwe && d.Nature == relation.ToString()).ToList();
            foreach (var thisrealtion in thischildrelations)
            {
                var parent = results.Count(d => d.PathCWe == cwe && d.Cwe == thisrealtion.RelatedCweID);
                if (parent > 0)
                {
                    Console.WriteLine($"Test {cwe} has {parent} {relation} matches of {thisrealtion.RelatedCweID}");
                }
            }
            return thischildrelations.Select(d => d.RelatedCweID).ToList();
        }
    }
}

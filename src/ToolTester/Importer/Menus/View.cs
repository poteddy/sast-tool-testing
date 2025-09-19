using Importer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                    var Catalogs = context.CWECatalogs.ToList();
                    List<Domain.Entities.Relationssship> relations = context.RelationsShips.ToList();
                    foreach (var c in Catalogs)
                    {

                        var cwesubPath = string.Format("CWE{0}_", c.Id);
                        var testpath = "D:\\github\\juliet\\testcases";
                        var cwePath = string.Format(@"testcases/" + cwesubPath);
                        var cwes = context.CWETestResults.Where(d => d.FilePath.StartsWith(cwePath) && d.Cwe == c.Id);
                        if (cwes.Count() > 0)
                        {
                            Console.WriteLine($"Test {c.Id} has {cwes.Count()} exact matches");
                        }
                        GetRelation(context, relations, RelatedNatureEnumeration.PeerOf, cwePath, c.Id);

                        var firstgenparents = await GetRelation(context, relations, RelatedNatureEnumeration.ParentOf, cwePath, c.Id);
                       // if(firstgenparents.Count() > 0)  Console.WriteLine($"Test grand parents of {c.Id}");
                        foreach (var i in firstgenparents)
                        {
                        
                            await GetRelation(context, relations, RelatedNatureEnumeration.ParentOf, cwePath, i);
                        }
                        var firstgenchildren = await GetRelation(context, relations, RelatedNatureEnumeration.ChildOf, cwePath, c.Id);
                      //  if(firstgenchildren.Count() >0) Console.WriteLine($"Test grand children of {c.Id}");
                        foreach (var i in firstgenparents)
                        {
                            
                            await GetRelation(context, relations, RelatedNatureEnumeration.ChildOf, cwePath, i);
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
        private async Task<List<int>> GetRelation(ApplicationDbContext context, List<Domain.Entities.Relationssship> relations, RelatedNatureEnumeration relation, string testpath, int cwe)
        {
            var thischildrelations = relations.Where(d => d.CWEID == cwe && d.Nature == relation.ToString()).ToList();
            foreach (var thisrealtion in thischildrelations)
            {
                var parent = context.CWETestResults.Where(d => d.FilePath.StartsWith(testpath) && d.Cwe == thisrealtion.RelatedCweID);
                if (parent.Count() > 0)
                {
                    Console.WriteLine($"Test {cwe} has {parent.Count()} {relation} matches of {thisrealtion.RelatedCweID}");
                }
            }
            return thischildrelations.Select(d => d.RelatedCweID).ToList();
        }
    }
}

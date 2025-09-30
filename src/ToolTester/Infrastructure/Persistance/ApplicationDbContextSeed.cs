using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolTester.Domain.Entities;

namespace ToolTester.Infrastructure.Persistance
{
    public static class ApplicationDbContextSeed
    {
        public static void SeedCWECatalog(this ApplicationDbContext context, Weakness_Catalog weakness_Catalog)
        {

            if (!context.CWECatalogs.Any())
            {
                var listcwe = new List<CWECatalog>();

                foreach (var weakness in weakness_Catalog.Weaknesses)
                {


                    if (!context.CWECatalogs.Any(d => d.Id == int.Parse(weakness.ID)) && !listcwe.Any(D => D.Id == int.Parse(weakness.ID)))
                    {
                        var cwe = new Domain.Entities.CWECatalog()
                        {
                            Abstraction = weakness.Abstraction.ToString(),
                            Description = weakness.Description,
                            Id = int.Parse(weakness.ID),
                            Name = weakness.Name,
                            Status = weakness.Status.ToString(),

                        };

                        context.CWECatalogs.Add(cwe);
                        if (weakness.Related_Weaknesses != null)
                        {
                            List<Relationssship> currentlist = new List<Relationssship>();
                            foreach (var dependant in weakness.Related_Weaknesses)
                            {
                                //some have different views like 672
                                if (currentlist.Any(x => x.RelatedCweID == int.Parse(dependant.CWE_ID) && x.Nature == dependant.Nature.ToString())) continue;
                                var cwedependant = new Domain.Entities.Relationssship()
                                {
                                    CWEID = cwe.Id,
                                    RelatedCweID = int.Parse(dependant.CWE_ID),
                                    ChainId = dependant.Chain_ID,
                                    Nature = dependant.Nature.ToString(),
                                    OrderSpecified = dependant.OrdinalSpecified,
                                    Oridinal = dependant.Ordinal.ToString(),

                                };
                                context.RelationsShips.Add(cwedependant);
                                currentlist.Add(cwedependant);
                            }
                        }
                        context.SaveChanges();

                    }

                }
                var childparentrel = context.RelationsShips.AsNoTracking().Where(d => d.Nature == RelatedNatureEnumeration.ChildOf.ToString());
                foreach (var child in childparentrel)
                {
                    var parent = new Relationssship()
                    {
                        CWEID = child.RelatedCweID,
                        RelatedCweID = child.CWEID,
                        Nature = RelatedNatureEnumeration.ParentOf.ToString(),
                        OrderSpecified = true,
                        Oridinal= OrdinalEnumeration.Primary.ToString()
                    };
                    context.RelationsShips.Add(parent);
                }

                var peerofrel = context.RelationsShips.AsNoTracking().Where(d => d.Nature == RelatedNatureEnumeration.PeerOf.ToString());
                foreach (var child in peerofrel)
                {
                    var parent = new Relationssship()
                    {
                        CWEID = child.RelatedCweID,
                        RelatedCweID = child.CWEID,
                        Nature = RelatedNatureEnumeration.PeerOf.ToString(),
                        OrderSpecified = false,
                        Oridinal = OrdinalEnumeration.Primary.ToString()
                    };
                    context.RelationsShips.Add(parent);
                }
                var followofrel = context.RelationsShips.AsNoTracking().Where(d => d.Nature == RelatedNatureEnumeration.CanPrecede.ToString());
                foreach (var child in followofrel)
                {
                    var parent = new Relationssship()
                    {
                        CWEID = child.RelatedCweID,
                        RelatedCweID = child.CWEID,
                        Nature = RelatedNatureEnumeration.CanFollow.ToString(),
                        OrderSpecified = false,
                        Oridinal = OrdinalEnumeration.Primary.ToString()
                    };
                    context.RelationsShips.Add(parent);
                }
                context.SaveChanges() ;
            }


        }
    }
    
}
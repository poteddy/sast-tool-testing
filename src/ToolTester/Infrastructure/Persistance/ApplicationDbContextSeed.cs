using AutoMapper;
using Microsoft.Diagnostics.Tracing.Parsers.FrameworkEventSource;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Common.Models;
using ToolTester.Domain.Entities;
using ToolTester.Infrastructure.Extensions;

namespace ToolTester.Infrastructure.Persistance
{
    public class ApplicationDbContextSeed : IApplicationDbContextSeed
    {
        private readonly IConfiguration _configuration;
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IZipfileService _zipfileService;

        public ApplicationDbContextSeed(IConfiguration configuration, IDbContextFactory<ApplicationDbContext> contextFactory, IZipfileService zipfileService)
        {
            _configuration = configuration;
            _contextFactory = contextFactory;
            _zipfileService = zipfileService;
        }
        public void SeedCWECatalog()
        {
            using (var _context = this._contextFactory.CreateDbContext())
            {
                try
                {
                    var mitrecatfilepath = _configuration.GetRequiredSection("MitreCatalogSetting").Get<MitreCatalogSetting>().Path;
                    if (!File.Exists(mitrecatfilepath))
                    {
                        Console.WriteLine();

                    }

                    var catalog = XMLExtensions.ReadXML(mitrecatfilepath);

                    Weakness_Catalog weakness_Catalog = catalog;
                    if (!_context.CWECatalogs.Any())
                    {
                        var listcwe = new List<CWECatalog>();

                        foreach (var weakness in weakness_Catalog.Weaknesses)
                        {


                            if (!_context.CWECatalogs.Any(d => d.Id == int.Parse(weakness.ID)) && !listcwe.Any(D => D.Id == int.Parse(weakness.ID)))
                            {
                                var cwe = new Domain.Entities.CWECatalog()
                                {
                                    Abstraction = weakness.Abstraction.ToString(),
                                    Description = weakness.Description,
                                    Id = int.Parse(weakness.ID),
                                    Name = weakness.Name,
                                    Status = weakness.Status.ToString(),

                                };
                                _context.CWECatalogs.Add(cwe);

                                var cweself = new Domain.Entities.Relationship()
                                {
                                    CWEID = cwe.Id,
                                    RelatedCweID = cwe.Id,

                                    Nature = RelatedNatureEnumeration.Self.ToString(),
                                    OrderSpecified =false,
                                    Oridinal = "missing"
                                };
                                _context.Relationships.Add(cweself);

                                if (weakness.Related_Weaknesses != null)
                                {
                                    List<Relationship> currentlist = new List<Relationship>();
                                    foreach (var dependant in weakness.Related_Weaknesses)
                                    {
                                        //some have different views like 672
                                        if (currentlist.Any(x => x.RelatedCweID == int.Parse(dependant.CWE_ID) && x.Nature == dependant.Nature.ToString())) continue;
                                        var cwedependant = new Domain.Entities.Relationship()
                                        {
                                            CWEID = cwe.Id,
                                            RelatedCweID = int.Parse(dependant.CWE_ID),
                                            ChainId = dependant.Chain_ID,
                                            Nature = dependant.Nature.ToString(),
                                            OrderSpecified = dependant.OrdinalSpecified,
                                            Oridinal = dependant.Ordinal.ToString(),

                                        };
                                        _context.Relationships.Add(cwedependant);
                                        currentlist.Add(cwedependant);
                                    }
                                }
                                _context.SaveChanges();

                            }

                        }
                        var childparentrel = _context.Relationships.AsNoTracking().Where(d => d.Nature == RelatedNatureEnumeration.ChildOf.ToString());
                        foreach (var child in childparentrel)
                        {
                            var parent = new Relationship()
                            {
                                CWEID = child.RelatedCweID,
                                RelatedCweID = child.CWEID,
                                Nature = RelatedNatureEnumeration.ParentOf.ToString(),
                                OrderSpecified = true,
                                Oridinal = OrdinalEnumeration.Primary.ToString()
                            };
                            _context.Relationships.Add(parent);
                        }

                        var peerofrel = _context.Relationships.AsNoTracking().Where(d => d.Nature == RelatedNatureEnumeration.PeerOf.ToString());
                        foreach (var child in peerofrel)
                        {
                            var parent = new Relationship()
                            {
                                CWEID = child.RelatedCweID,
                                RelatedCweID = child.CWEID,
                                Nature = RelatedNatureEnumeration.PeerOf.ToString(),
                                OrderSpecified = false,
                                Oridinal = OrdinalEnumeration.Primary.ToString()
                            };
                            _context.Relationships.Add(parent);
                        }
                        var followofrel = _context.Relationships.AsNoTracking().Where(d => d.Nature == RelatedNatureEnumeration.CanPrecede.ToString());
                        foreach (var child in followofrel)
                        {
                            var parent = new Relationship()
                            {
                                CWEID = child.RelatedCweID,
                                RelatedCweID = child.CWEID,
                                Nature = RelatedNatureEnumeration.CanFollow.ToString(),
                                OrderSpecified = false,
                                Oridinal = OrdinalEnumeration.Primary.ToString()
                            };
                            _context.Relationships.Add(parent);
                        }
                        _context.SaveChanges();
                    }
                    if (!_context.JulietCoverages.Any())
                    {
                      
                        var julietpath = _configuration.GetRequiredSection("JulietProjectSetting").Get<JulietProjectSetting>().Path;
                        var flist = _zipfileService.FileCount(julietpath);
                        foreach (var f in flist)
                        {
                          
                                var Juliet = new JulietCoverage()
                                {
                                    CWE_ID = f.Key,
                                    Covered = f.Value
                                };
                                _context.JulietCoverages.Add(Juliet);
                            
                          
                        }
                        _context.SaveChanges(true);
                    }
                }
                catch (Exception)
                {

                    throw;
                }


            }
        }
    }

}
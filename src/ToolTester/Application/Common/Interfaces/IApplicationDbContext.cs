using ToolTester.Domain;
using Microsoft.EntityFrameworkCore;
using ToolTester.Domain.Entities;

namespace ToolTester.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<CWECatalog> CWECatalogs { get; set; }
        DbSet<Relationssship> RelationsShips { get; set; }

    }
}
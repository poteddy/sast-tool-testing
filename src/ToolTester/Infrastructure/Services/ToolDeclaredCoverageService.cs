using Microsoft.EntityFrameworkCore;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Domain.Entities;
using ToolTester.Infrastructure.Persistance;

namespace ToolTester.Infrastructure.Services;

public class ToolDeclaredCoverageService : IToolDeclaredCoverageService
{
    private readonly ApplicationDbContext _context;

    public ToolDeclaredCoverageService(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<Tool>> GetAllToolsAsync()
    {
        return await _context.Tools
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ToolDeclaredCwe>> GetDeclaredForToolAsync(int toolId)
    {
        return await _context.ToolDeclaredCwes
            .AsNoTracking()
            .Where(td => td.ToolId == toolId)
            .OrderBy(td => td.CweId)
            .ToListAsync();
    }

    public async Task<ToolDeclaredCwe?> AddDeclaredAsync(ToolDeclaredCwe declared)
    {
        if (declared is null) throw new ArgumentNullException(nameof(declared));

        // Prevent duplicates: return existing if present
        var existing = await _context.ToolDeclaredCwes
            .FirstOrDefaultAsync(td => td.ToolId == declared.ToolId && td.CweId == declared.CweId);

        if (existing is not null) return existing;

        _context.ToolDeclaredCwes.Add(declared);
        await _context.SaveChangesAsync();
        return declared;
    }

    public async Task<bool> RemoveDeclaredAsync(int declaredId)
    {
        var entity = await _context.ToolDeclaredCwes.FindAsync(declaredId);
        if (entity is null) return false;

        _context.ToolDeclaredCwes.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
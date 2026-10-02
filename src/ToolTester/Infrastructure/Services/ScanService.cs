using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Domain.Entities;
using ToolTester.Infrastructure.Persistance;

namespace ToolTester.Infrastructure.Services;

public class ScanService : IScanService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<ScanService> _logger;

    public ScanService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<ScanService> logger)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<Scan>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await ctx.Scans
            .AsNoTracking()
            .Include(s => s.Tool)
            .ToListAsync(cancellationToken);
    }

    public async Task<Scan?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await ctx.Scans
            .Include(s => s.Tool)
            .Include(s => s.TestResults)
            .Include(s => s.Reports)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Scan> CreateAsync(Scan scan, CancellationToken cancellationToken = default)
    {
        if (scan is null) throw new ArgumentNullException(nameof(scan));
        await using var ctx = await _contextFactory.CreateDbContextAsync(cancellationToken);
        ctx.Scans.Add(scan);
        await ctx.SaveChangesAsync(cancellationToken);
        return scan;
    }

    public async Task UpdateAsync(Scan scan, CancellationToken cancellationToken = default)
    {
        if (scan is null) throw new ArgumentNullException(nameof(scan));
        await using var ctx = await _contextFactory.CreateDbContextAsync(cancellationToken);
        ctx.Scans.Update(scan);
        await ctx.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await ctx.Scans.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null) return;
        ctx.Scans.Remove(entity);
        await ctx.SaveChangesAsync(cancellationToken);
    }
}
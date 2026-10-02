using ToolTester.Domain.Entities;
namespace ToolTester.Application.Common.Interfaces;

public interface IScanService
{
    Task<List<Scan>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Scan?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Scan> CreateAsync(Scan scan, CancellationToken cancellationToken = default);
    Task UpdateAsync(Scan scan, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
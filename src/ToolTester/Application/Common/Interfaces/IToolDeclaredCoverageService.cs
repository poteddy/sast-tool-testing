using ToolTester.Domain.Entities;

public interface IToolDeclaredCoverageService
{
    Task<IReadOnlyList<Tool>> GetAllToolsAsync();
    Task<IReadOnlyList<ToolDeclaredCwe>> GetDeclaredForToolAsync(int toolId);
    Task<ToolDeclaredCwe?> AddDeclaredAsync(ToolDeclaredCwe declared);
    Task<bool> RemoveDeclaredAsync(int declaredId);
    Task SaveChangesAsync();
}
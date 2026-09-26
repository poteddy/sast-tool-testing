namespace ToolTester.Application.Common.Interfaces
{
    public interface IParsingService:IDisposable
    {
      
        Task<int> Parse(int toolId, string filePath, CancellationToken cancellationToken = default);
    }
}
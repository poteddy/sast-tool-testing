namespace ToolTester.Domain.Coomon.Interfaces
{
    public interface IParsingService:IDisposable
    {
        Task<int> Parse(int ToolId, string filepath);
    }
}
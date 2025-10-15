namespace ToolTester.Application.Common.Interfaces
{
    public interface IParsingService:IDisposable
    {
        Task<int> Parse(int ToolId, string filepath);
    }
}
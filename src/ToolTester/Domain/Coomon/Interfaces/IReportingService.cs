using System.Text;

namespace ToolTester.Domain.Coomon.Interfaces
{
    public interface IReportingService : IDisposable
    {
        Task<StringBuilder> GenerateReport(int testid);
    }
}

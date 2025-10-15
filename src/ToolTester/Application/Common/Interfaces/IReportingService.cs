using System.Text;

namespace ToolTester.Application.Common.Interfaces
{
    public interface IReportingService : IDisposable
    {
        Task<StringBuilder> GenerateReport(int scanid, int toolid);
    }
}

using ToolTester.Application.Reports.DTO;

namespace ToolTester.Application.Common.Interfaces
{
    public interface IFalsePositiveQuery
    {
        Task<FalsePositiveChartDto> GetFalsePositiveChartAsync(CancellationToken cancellationToken = default);
    }
}

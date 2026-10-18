using ToolTester.Application.Reports.DTO;

namespace ToolTester.Application.Common.Interfaces
{
    public interface IHeatmapQuery
    {
        Task<HeatmapDto> GetHeatmapAsync(CancellationToken cancellationToken = default);
    }
    public interface IScanDisplayNameProvider
    {
        Task LoadAsync(CancellationToken cancellationToken = default);
        string GetScanName(int scanId);
        string GetScanDisplayName(int scanId);
    }
}

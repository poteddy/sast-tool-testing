namespace ToolTester.Presentation.Services
{
    public interface IReportQueryService
    {
        Task<ReportQueryResult> GetReportDataAsync(CancellationToken cancellationToken = default);
    }
}
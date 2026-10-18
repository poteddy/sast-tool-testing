using MediatR;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.CWETestResultBases.Queries;
using ToolTester.Application.Reports.Quiries;
using ToolTester.Presentation.Models;
using ToolTester.Presentation.PageModels;

namespace ToolTester.Presentation.Services
{
    public class ReportQueryService : IReportQueryService
    {
        private readonly IMediator _mediator;
        private readonly IScanDisplayNameProvider _scanNameProvider;

        public ReportQueryService(IMediator mediator, IScanDisplayNameProvider scanNameProvider)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _scanNameProvider = scanNameProvider ?? throw new ArgumentNullException(nameof(scanNameProvider));
        }

        public async Task<ReportQueryResult> GetReportDataAsync(CancellationToken cancellationToken = default)
        {
            // Keep paging constants aligned with original VM
            const int TestResultPageSize = 250_000;
            const int ReportPageSize = 10_000;

            var testResult = await _mediator.Send(
                new GetCweTestResultBasesWithPaginationQuery { PageSize = TestResultPageSize },
                cancellationToken);

            var reportResult = await _mediator.Send(
                new GetReportsWithPaginationQuery { PageSize = ReportPageSize },
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            var reportLookup = reportResult.Items
                .GroupBy(report => (report.ScanId, ScannerCweId: report.ScannerCweId, GroundTruthCweId: report.GroundTruthCweId))
                .ToDictionary(group => group.Key, group => group.First());

            // Fix: Use the provider method instead of invoking the provider instance
            var relatedSeries = reportResult.Items
                .GroupBy(report => report.ScanId)
                .OrderBy(group => group.Key)
                .Select(group => new RelatedSeries
                {
                    ScanId = group.Key,
                    ScanName = _scanNameProvider.GetScanName(group.Key), // <-- corrected
                    Items = group
                        .OrderBy(report => report.GroundTruthCweId)
                        .ThenBy(report => report.ScannerCweId)
                        .Select(report => new RelatedItemsInTest
                        {
                            Id = report.Id,
                            GroundTruthCweId = report.GroundTruthCweId,
                            ScannerCweId = report.ScannerCweId,
                            ScanId = report.ScanId,
                            ToolId = report.ToolId,
                            Count = report.Count,
                            Relationship = report.Relationship,
                            RelationshipScore = report.RelationshipScore
                        })
                        .ToList()
                })
                .ToList();

            var mappedItems = testResult.Items
                .Select(item =>
                {
                    var reportKey = (ScanId: item.ScanId, ScannerCweId: item.ScannerFoundCWE, GroundTruthCweId: item.GroundTruthCwe);

                    var hasSavedReport = reportLookup.TryGetValue(reportKey, out var savedReport);

                    var detectedWithoutCwe =
                        !item.ScannerFoundCWE.HasValue ||
                        (hasSavedReport &&
                         string.Equals(savedReport!.Relationship, nameof(CweRelationshipKind.DetectedWithoutCwe), StringComparison.OrdinalIgnoreCase));

                    var errorValue =
                        detectedWithoutCwe ? 2 :
                        hasSavedReport && savedReport!.RelationshipScore > 0 ? 0 : 5;

                    return new CweTestResults
                    {
                        Id = item.Id,
                        ScanId = item.ScanId,
                        TestId = item.TestId,
                        ScannerFoundCWE = item.ScannerFoundCWE,
                        GroundTruthCwe = item.GroundTruthCwe,
                        TestFileName = item.TestFileName,
                        TestPath = item.TestPath,
                        ErrorValue = errorValue
                    };
                })
                .ToList();

            var testSeries = mappedItems
                .GroupBy(item => item.ScanId)
                .OrderBy(group => group.Key)
                .Select(group => new TestSeries
                {
                    ScanId = group.Key,
                    ScanName = _scanNameProvider.GetScanName(group.Key),
                    Items = group.ToList()
                })
                .ToList();

            var aggregatedSeries = testResult.Items
                .GroupBy(item => item.ScanId)
                .OrderBy(group => group.Key)
                .Select(group => new AggrigatedSeries
                {
                    ScanId = group.Key,
                    ScanName = _scanNameProvider.GetScanName(group.Key),
                    Items = group
                        .Where(item => item.ScannerFoundCWE.HasValue)
                        .GroupBy(item => item.ScannerFoundCWE!.Value)
                        .OrderBy(cweGroup => cweGroup.Key)
                        .Select(cweGroup => new AggrigatedItems { CweId = cweGroup.Key, Count = cweGroup.Count() })
                        .ToList()
                })
                .ToList();

            return new ReportQueryResult(
                relatedSeries,
                mappedItems,
                testSeries,
                aggregatedSeries);
        }
    }
}
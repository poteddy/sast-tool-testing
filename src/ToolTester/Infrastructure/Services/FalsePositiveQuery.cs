using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Reports;
using ToolTester.Application.Reports.DTO;
using ToolTester.Application.Reports.Quiries; // existing query used by ReportPageModel

namespace ToolTester.Infrastructure.Services
{
    // Builds an application DTO describing the false-positive series per scan.
    public class FalsePositiveQuery : IFalsePositiveQuery
    {
        private readonly IMediator _mediator;
        private readonly IScanDisplayNameProvider _scanNameProvider;

        public FalsePositiveQuery(IMediator mediator, IScanDisplayNameProvider scanNameProvider)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _scanNameProvider = scanNameProvider ?? throw new ArgumentNullException(nameof(scanNameProvider));
        }

        public async Task<FalsePositiveChartDto> GetFalsePositiveChartAsync(CancellationToken cancellationToken = default)
        {
            // Use the same reports query used by ReportPageModel to get saved reports
            var reportResult = await _mediator.Send(new GetReportsWithPaginationQuery
            {
                PageSize = 10000
            }, cancellationToken);

            // Filter out rows without a numeric ScannerCweId, then group by scan and scannerCweId and sum counts
            var groupedByScan = reportResult.Items
                .Where(r => r.ScannerCweId.HasValue)           // ensure non-null CWE id
                .GroupBy(r => r.ScanId)
                .OrderBy(g => g.Key);

            var seriesList = new List<ScanFalsePositiveSeriesDto>();

            foreach (var scanGroup in groupedByScan)
            {
                var scanId = scanGroup.Key;
                var scanName = _scanNameProvider.GetDisplayName(scanId);

                var points = scanGroup
                    .GroupBy(r => r.ScannerCweId!.Value) // safe: filtered above
                    .Select(g => new FalsePositivePointDto
                    {
                        CweId = g.Key,
                        Count = g.Sum(x => x.Count)
                    })
                    .Where(p => p.CweId > 0) // only numeric CWEs (optional)
                    .OrderBy(p => p.CweId)
                    .ToList();

                seriesList.Add(new ScanFalsePositiveSeriesDto
                {
                    ScanId = scanId,
                    ScanName = scanName,
                    Points = points
                });
            }

            return new FalsePositiveChartDto
            {
                Series = seriesList
            };
        }
    }
}
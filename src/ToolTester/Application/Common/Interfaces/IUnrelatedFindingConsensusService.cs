

using ToolTester.Application.Reports.DTO;

namespace ToolTester.Application.Common.Interfaces;

public interface IUnrelatedFindingConsensusService
{
    Task<IReadOnlyList<UnrelatedFindingConsensus>>
        GetConsensusAsync(
            int minimumScannerCount = 2,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UnrelatedFindingConsensus>>
        GetConsensusForGroundTruthCweAsync(
            int groundTruthCweId,
            int minimumScannerCount = 2,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UnrelatedFindingConsensus>>
        GetAllUnrelatedAsync(
            CancellationToken cancellationToken = default);
}
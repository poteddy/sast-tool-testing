namespace ToolTester.Application.Common.Interfaces;

public interface ICweRootCauseResolver
{
    Task<int?> ResolveMatchedTargetCweAsync(
        int scannerCweId,
        int? groundTruthCweId = null,
        CancellationToken cancellationToken = default);
}
using ToolTester.Application.CweRelationshipEngine;

public interface ICweTopologyService
{
    Task<CweTopologyMatch> GetRelationshipAsync(
        int sourceCweId,
        int targetCweId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CweTopologyNode>> GetAncestorsAsync(
        int cweId,
        CweAbstractionLevel? abstraction = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CweTopologyNode>> GetDescendantsAsync(
        int cweId,
        CweAbstractionLevel? abstraction = null,
        CancellationToken cancellationToken = default);
}

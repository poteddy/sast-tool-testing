namespace ToolTester.Infrastructure.Services
{
    public interface ICweRelationshipService
    {
        Task<RelationshipResult> EvaluateAsync(
            int scannerCweId,
            int groundTruthCweId,
            string? scannerRuleId = null,
            string? programmingLanguage = null,
            CancellationToken cancellationToken = default);
    }
}

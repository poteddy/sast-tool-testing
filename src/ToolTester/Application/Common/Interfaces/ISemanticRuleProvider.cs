namespace ToolTester.Application.Common.Interfaces
{
    public interface ISemanticRuleProvider
    {
        IReadOnlyCollection<SemanticRule> Rules { get; }
        Task ReloadAsync(CancellationToken cancellationToken = default);
    }
}

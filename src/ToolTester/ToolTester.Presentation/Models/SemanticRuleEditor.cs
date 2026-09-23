using CommunityToolkit.Mvvm.ComponentModel;
using CweRelationshipEngine;

namespace ToolTester.Presentation.PageModels;

public partial class SemanticRuleEditor : ObservableObject
{
    [ObservableProperty]
    private int sourceCweId;

    [ObservableProperty]
    private int targetCweId;

    [ObservableProperty]
    private CweRelationshipKind relationship;

    [ObservableProperty]
    private int score;

    [ObservableProperty]
    private string rationale = string.Empty;

    [ObservableProperty]
    private string evidenceReference = string.Empty;

    [ObservableProperty]
    private bool bidirectional;

    public SemanticRule ToSemanticRule()
    {
        return new SemanticRule(
            SourceCweId: SourceCweId,
            TargetCweId: TargetCweId,
            Relationship: Relationship,
            Score: Score,
            Rationale: Rationale.Trim(),
            EvidenceReference: EvidenceReference.Trim(),
            Bidirectional: Bidirectional);
    }

    public static SemanticRuleEditor FromSemanticRule(SemanticRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        return new SemanticRuleEditor
        {
            SourceCweId = rule.SourceCweId,
            TargetCweId = rule.TargetCweId,
            Relationship = rule.Relationship,
            Score = rule.Score,
            Rationale = rule.Rationale,
            EvidenceReference = rule.EvidenceReference,
            Bidirectional = rule.Bidirectional
        };
    }
}
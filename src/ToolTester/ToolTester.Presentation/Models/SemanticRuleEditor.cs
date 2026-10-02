using CommunityToolkit.Mvvm.ComponentModel;

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

    // New properties that reflect the database entity
    [ObservableProperty]
    private string scannerRuleId = string.Empty;

    [ObservableProperty]
    private string programmingLanguage = string.Empty;

    [ObservableProperty]
    private bool enabled = true;

    [ObservableProperty]
    private int version = 1;

    [ObservableProperty]
    private bool isCustom = true;

    public SemanticRule ToSemanticRule()
    {
        return new SemanticRule(
            SourceCweId: SourceCweId,
            TargetCweId: TargetCweId,
            Relationship: Relationship,
            Score: Score,
            Rationale: Rationale.Trim(),
            EvidenceReference: EvidenceReference.Trim(),
            ScannerRuleId: string.IsNullOrWhiteSpace(ScannerRuleId) ? null : ScannerRuleId.Trim(),
            ProgrammingLanguage: string.IsNullOrWhiteSpace(ProgrammingLanguage) ? null : ProgrammingLanguage.Trim(),
            Bidirectional: Bidirectional,
            Enabled: Enabled,
            Version: Version,
            IsCustom: IsCustom);
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
            Bidirectional = rule.Bidirectional,
            ScannerRuleId = rule.ScannerRuleId ?? string.Empty,
            ProgrammingLanguage = rule.ProgrammingLanguage ?? string.Empty,
            Enabled = rule.Enabled,
            Version = rule.Version,
            IsCustom = rule.IsCustom

        };
    }
}
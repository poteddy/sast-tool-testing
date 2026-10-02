using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Text.Json;
using ToolTester.Domain.Entities;
using ToolTester.Infrastructure.Persistance;

namespace ToolTester.Presentation.PageModels;

public partial class SemanticRulesViewModel : ObservableObject
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private int selectedRuleIndex = -1;
    private readonly List<SemanticRule> _allRules = new();

    [ObservableProperty]
    private SemanticRuleEditor currentRule = new();

    [ObservableProperty]
    private SemanticRule? selectedRule;

    [ObservableProperty]
    private string? validationMessage;

    // Search text property — updates filter when changed
    [ObservableProperty]
    private string searchText = string.Empty;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    public SemanticRulesViewModel(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));

        _ = LoadRulesAsync();
    }

    public IReadOnlyList<CweRelationshipKind> RelationshipTypes { get; } =
        Enum.GetValues<CweRelationshipKind>();

    public ObservableCollection<SemanticRule> Rules { get; } = new();

    private async Task LoadRulesAsync()
    {
        try
        {
            _allRules.Clear();
            Rules.Clear();

            await using var context = await _contextFactory.CreateDbContextAsync();

            var entities = await context.CweSemanticRules
                .AsNoTracking()
                .ToListAsync();

            foreach (var e in entities)
            {
                if (!Enum.TryParse<CweRelationshipKind>(e.Relationship, out var relationship))
                {
                    continue;
                }

                var record = new SemanticRule(
                    SourceCweId: e.SourceCweId,
                    TargetCweId: e.TargetCweId,
                    Relationship: relationship,
                    Score: e.Score,
                    Rationale: e.Rationale,
                    EvidenceReference: e.EvidenceReference,
                    ScannerRuleId: string.IsNullOrWhiteSpace(e.ScannerRuleId) ? null : e.ScannerRuleId,
                    ProgrammingLanguage: string.IsNullOrWhiteSpace(e.ProgrammingLanguage) ? null : e.ProgrammingLanguage,
                    Bidirectional: e.Bidirectional,
                    Enabled: e.Enabled,
                    Version: e.Version,
                    IsCustom: e.IsCustom
                    );

                _allRules.Add(record);
            }

            // fallback to file/default logic if DB empty (preserves previous behavior)
            if (!_allRules.Any())
            {
                string path = GetRulesFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var fileRules = JsonSerializer.Deserialize<List<SemanticRule>>(json);
                    if (fileRules is not null)
                    {
                        _allRules.AddRange(fileRules);
                        // persist them to DB
                        foreach (var r in fileRules)
                        {
                            await using var ctx = await _contextFactory.CreateDbContextAsync();
                            await UpsertSemanticRuleEntityAsync(r, ctx);
                            await ctx.SaveChangesAsync();
                        }
                    }
                }
                else
                {
                    LoadDefaultRules();
                    foreach (var r in Rules)
                    {
                        _allRules.Add(r);
                        await using var ctx = await _contextFactory.CreateDbContextAsync();
                        await UpsertSemanticRuleEntityAsync(r, ctx);
                        await ctx.SaveChangesAsync();
                    }
                }
            }

            ApplyFilter();
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Failed to load rules: {ex.Message}";
        }
    }

    private void LoadDefaultRules()
    {
        _allRules.Add(
            new SemanticRule(
                SourceCweId: 676,
                TargetCweId: 121,
                Relationship:
                    CweRelationshipKind.SameRootCauseBroaderCwe,
                Score: 850,
                Rationale:
                    "The scanner reported dangerous-function usage while the benchmark expects a stack-based buffer overflow. " +
                    "Treat this as a reviewed causal mapping only when finding-level evidence shows that the reported function " +
                    "use is the mechanism for the expected overflow.",
                EvidenceReference:
                    "Internal normalization rule CWE-676-to-CWE-121 v1",
                Bidirectional: true));
    }

    private void ApplyFilter()
    {
        Rules.Clear();

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            foreach (var r in _allRules)
                Rules.Add(r);

            return;
        }

        var q = SearchText.Trim();

        bool isNumeric = int.TryParse(q, out var qnum);

        foreach (var r in _allRules)
        {
            if (isNumeric &&
                (r.SourceCweId == qnum || r.TargetCweId == qnum))
            {
                Rules.Add(r);
                continue;
            }

            if (r.Relationship.ToString().Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(r.ScannerRuleId) && r.ScannerRuleId.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(r.ProgrammingLanguage) && r.ProgrammingLanguage.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(r.Rationale) && r.Rationale.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(r.EvidenceReference) && r.EvidenceReference.Contains(q, StringComparison.OrdinalIgnoreCase)))
            {
                Rules.Add(r);
            }
        }
    }

    [RelayCommand]
    private void NewRule()
    {
        ResetEditor();
    }

    [RelayCommand]
    private async Task SaveRule()
    {
        ValidationMessage = ValidateRule();

        if (ValidationMessage is not null)
        {
            return;
        }

        SemanticRule savedRule = CurrentRule.ToSemanticRule();

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            if (selectedRuleIndex < 0)
            {
                _allRules.Add(savedRule);
            }
            else if (selectedRuleIndex < _allRules.Count)
            {
                _allRules[selectedRuleIndex] = savedRule;
            }

            await UpsertSemanticRuleEntityAsync(savedRule, context);
            await context.SaveChangesAsync();

            SaveRules();
            ApplyFilter();
            ResetEditor();
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Save failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void EditRule(SemanticRule? rule)
    {
        if (rule is null) return;

        selectedRuleIndex = FindReferenceIndex(rule);

        if (selectedRuleIndex < 0) return;

        SelectedRule = rule;
        CurrentRule = SemanticRuleEditor.FromSemanticRule(rule);
        ValidationMessage = null;
    }

    [RelayCommand]
    private async Task DeleteRule(SemanticRule? rule)
    {
        if (rule is null) return;

        int index = FindReferenceIndex(rule);

        if (index < 0) return;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            string relationshipStr = rule.Relationship.ToString();

            var entity = await context.CweSemanticRules.FirstOrDefaultAsync(e =>
                e.SourceCweId == rule.SourceCweId &&
                e.TargetCweId == rule.TargetCweId &&
                e.Relationship == relationshipStr &&
                (e.ScannerRuleId ?? string.Empty) == (rule.ScannerRuleId ?? string.Empty) &&
                (e.ProgrammingLanguage ?? string.Empty) == (rule.ProgrammingLanguage ?? string.Empty) &&
                e.Version == rule.Version);

            if (entity is not null)
            {
                context.CweSemanticRules.Remove(entity);
                await context.SaveChangesAsync();
            }

            _allRules.RemoveAt(index);
            SaveRules();
            ApplyFilter();

            if (index == selectedRuleIndex)
            {
                ResetEditor();
            }
            else if (index < selectedRuleIndex)
            {
                selectedRuleIndex--;
            }
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Delete failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        ResetEditor();
    }

    [RelayCommand]
    private async Task ImportRules()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(
                new PickOptions
                {
                    PickerTitle = "Select Semantic Rules JSON"
                });

            if (result is null) return;

            await using var stream = await result.OpenReadAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            options.Converters.Add(
                new System.Text.Json.Serialization
                    .JsonStringEnumConverter());

            var importedRules =
                await JsonSerializer.DeserializeAsync<
                    List<SemanticRule>>(
                    stream,
                    options);
            
            if (importedRules is null)
            {
                ValidationMessage = "The selected file contained no rules.";
                return;
            }
         
            await using var context = await _contextFactory.CreateDbContextAsync();
            int importedCount = 0;

            foreach (var rule in importedRules)
            {
                var customRule = rule with
                {
                    IsCustom = true
                };

                if (!_allRules.Any(existing =>
                    existing.SourceCweId == customRule.SourceCweId &&
                    existing.TargetCweId == customRule.TargetCweId &&
                    existing.Relationship == customRule.Relationship &&
                    (existing.ScannerRuleId ?? string.Empty) == (customRule.ScannerRuleId ?? string.Empty) &&
                    (existing.ProgrammingLanguage ?? string.Empty) == (customRule.ProgrammingLanguage ?? string.Empty) &&
                    existing.Version == customRule.Version))
                {
                    _allRules.Add(customRule);

                    await UpsertSemanticRuleEntityAsync(
                        customRule,
                        context);

                    importedCount++;
                }
            }

            if (context.ChangeTracker.HasChanges())
            {
                await context.SaveChangesAsync();
            }

            SaveRules();
            ApplyFilter();

            ValidationMessage = $"Imported {importedCount} new rule(s).";
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Import failed: {ex.Message}";
        }
    }

    private async Task UpsertSemanticRuleEntityAsync(SemanticRule rule, ApplicationDbContext context)
    {
        string relationshipStr = rule.Relationship.ToString();
        string scannerId = rule.ScannerRuleId ?? string.Empty;
        string lang = rule.ProgrammingLanguage ?? string.Empty;

        var existing = await context.CweSemanticRules.FirstOrDefaultAsync(e =>
            e.SourceCweId == rule.SourceCweId &&
            e.TargetCweId == rule.TargetCweId &&
            e.Relationship == relationshipStr &&
            (e.ScannerRuleId ?? string.Empty) == scannerId &&
            (e.ProgrammingLanguage ?? string.Empty) == lang &&
            e.Version == rule.Version);

        if (existing is null)
        {
            var entity = new CweSemanticRule
            {
                SourceCweId = rule.SourceCweId,
                TargetCweId = rule.TargetCweId,
                Relationship = relationshipStr,
                Score = rule.Score,
                Rationale = rule.Rationale.Trim(),
                EvidenceReference = rule.EvidenceReference.Trim(),
                ScannerRuleId = string.IsNullOrWhiteSpace(rule.ScannerRuleId) ? null : rule.ScannerRuleId,
                ProgrammingLanguage = string.IsNullOrWhiteSpace(rule.ProgrammingLanguage) ? null : rule.ProgrammingLanguage,
                Bidirectional = rule.Bidirectional,
                Enabled = rule.Enabled,
                Version = rule.Version
            };

            context.CweSemanticRules.Add(entity);
        }
        else
        {
            existing.Score = rule.Score;
            existing.Rationale = rule.Rationale.Trim();
            existing.EvidenceReference = rule.EvidenceReference.Trim();
            existing.ScannerRuleId = string.IsNullOrWhiteSpace(rule.ScannerRuleId) ? null : rule.ScannerRuleId;
            existing.ProgrammingLanguage = string.IsNullOrWhiteSpace(rule.ProgrammingLanguage) ? null : rule.ProgrammingLanguage;
            existing.Bidirectional = rule.Bidirectional;
            existing.Enabled = rule.Enabled;
            existing.Version = rule.Version;
        }
    }

    private static string GetRulesFilePath()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments),
            "ToolTester");

        Directory.CreateDirectory(folder);

        return Path.Combine(folder, "semantic-rules.json");
    }

    private void SaveRules()
    {
        var customRules = _allRules
.Where(x => x.IsCustom)
.ToList();
        string json = JsonSerializer.Serialize(
            customRules,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(GetRulesFilePath(), json);
    }

    private int FindReferenceIndex(SemanticRule rule)
    {
        for (int i = 0; i < _allRules.Count; i++)
        {
            var r = _allRules[i];

            if (r.SourceCweId == rule.SourceCweId &&
                r.TargetCweId == rule.TargetCweId &&
                r.Relationship == rule.Relationship &&
                (r.ScannerRuleId ?? string.Empty) == (rule.ScannerRuleId ?? string.Empty) &&
                (r.ProgrammingLanguage ?? string.Empty) == (rule.ProgrammingLanguage ?? string.Empty) &&
                r.Version == rule.Version)
            {
                return i;
            }
        }

        return -1;
    }

    private void ResetEditor()
    {
        CurrentRule = new SemanticRuleEditor();
        SelectedRule = null;
        selectedRuleIndex = -1;
        ValidationMessage = null;
    }

    private string? ValidateRule()
    {
        if (CurrentRule.SourceCweId <= 0) return "Source CWE must be greater than zero.";
        if (CurrentRule.TargetCweId <= 0) return "Target CWE must be greater than zero.";
        if (CurrentRule.Score is < 0 or > 1000) return "Score must be between 0 and 1000.";
        if (string.IsNullOrWhiteSpace(CurrentRule.Rationale)) return "Rationale is required.";
        if (string.IsNullOrWhiteSpace(CurrentRule.EvidenceReference)) return "Evidence reference is required.";
        return null;
    }
}
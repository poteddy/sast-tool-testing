using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CweRelationshipEngine;
using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.Maui.Storage;
namespace ToolTester.Presentation.PageModels;

public partial class SemanticRulesViewModel : ObservableObject
{

    private int selectedRuleIndex = -1;

    [ObservableProperty]
    private SemanticRuleEditor currentRule = new();

    [ObservableProperty]
    private SemanticRule? selectedRule;

    [ObservableProperty]
    private string? validationMessage;

    public SemanticRulesViewModel()
    {
        LoadRules();
    }


    public IReadOnlyList<CweRelationshipKind> RelationshipTypes { get; } =
        Enum.GetValues<CweRelationshipKind>();

    public ObservableCollection<SemanticRule> Rules { get; } = [];
    //public ObservableCollection<SemanticRule> Rules { get; } =
    //[
    //    new SemanticRule(
    //        SourceCweId: 676,
    //        TargetCweId: 121,
    //        Relationship:
    //            CweRelationshipKind.SameRootCauseBroaderCwe,
    //        Score: 850,
    //        Rationale:
    //            "The scanner reported dangerous-function usage while the benchmark expects a stack-based buffer overflow. " +
    //            "Treat this as a reviewed causal mapping only when finding-level evidence shows that the reported function " +
    //            "use is the mechanism for the expected overflow.",
    //        EvidenceReference:
    //            "Internal normalization rule CWE-676-to-CWE-121 v1",
    //        Bidirectional: true)
    //];
    private void LoadRules()
    {
        string path = GetRulesFilePath();

        if (!File.Exists(path))
        {
            LoadDefaultRules();
            SaveRules();
            return;
        }

        string json = File.ReadAllText(path);

        List<SemanticRule>? rules =
            JsonSerializer.Deserialize<List<SemanticRule>>(json);

        Rules.Clear();

        if (rules is not null)
        {
            foreach (SemanticRule rule in rules)
            {
                Rules.Add(rule);
            }
        }
    }
    private void SaveRules()
    {
        string json = JsonSerializer.Serialize(
            Rules,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(GetRulesFilePath(), json);
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
    private void LoadDefaultRules()
    {
        Rules.Add(
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
    [RelayCommand]
    private void NewRule()
    {
        ResetEditor();
    }

    [RelayCommand]
    private void SaveRule()
    {
        ValidationMessage = ValidateRule();

        if (ValidationMessage is not null)
        {
            return;
        }

        SemanticRule savedRule = CurrentRule.ToSemanticRule();

        if (selectedRuleIndex < 0)
        {
            Rules.Add(savedRule);
        }
        else if (selectedRuleIndex < Rules.Count)
        {
            Rules[selectedRuleIndex] = savedRule;
        }

        SaveRules();

        ResetEditor();
    }

    [RelayCommand]
    private void EditRule(SemanticRule? rule)
    {
        if (rule is null)
        {
            return;
        }

        selectedRuleIndex = FindReferenceIndex(rule);

        if (selectedRuleIndex < 0)
        {
            return;
        }

        SelectedRule = rule;
        CurrentRule = SemanticRuleEditor.FromSemanticRule(rule);
        ValidationMessage = null;
    }

    [RelayCommand]
    private void DeleteRule(SemanticRule? rule)
    {
        if (rule is null)
        {
            return;
        }

        int index = FindReferenceIndex(rule);

        if (index < 0)
        {
            return;
        }

        Rules.RemoveAt(index);

        SaveRules();

        if (index == selectedRuleIndex)
        {
            ResetEditor();
        }
        else if (index < selectedRuleIndex)
        {
            selectedRuleIndex--;
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

            if (result is null)
            {
                return;
            }

            await using var stream =
                await result.OpenReadAsync();

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
                ValidationMessage =
                    "The selected file contained no rules.";

                return;
            }

            foreach (var rule in importedRules)
            {
                if (!Rules.Any(existing =>
                    existing.SourceCweId == rule.SourceCweId &&
                    existing.TargetCweId == rule.TargetCweId &&
                    existing.Relationship == rule.Relationship))
                {
                    Rules.Add(rule);
                }
            }

            SaveRules();

            ValidationMessage =
                $"Imported {Rules.Count} rule(s).";
        }
        catch (Exception ex)
        {
            ValidationMessage =
                $"Import failed: {ex.Message}";
        }
    }
    [RelayCommand]
    private async Task ExportRules()
    {
        try
        {
            SaveRules();

            ValidationMessage =
                $"Exported {Rules.Count} rule(s) to {GetRulesFilePath()}";
        }
        catch (Exception ex)
        {
            ValidationMessage =
                $"Export failed: {ex.Message}";
        }
    }
    private string? ValidateRule()
    {
        if (CurrentRule.SourceCweId <= 0)
        {
            return "Source CWE ID must be greater than zero.";
        }

        if (CurrentRule.TargetCweId <= 0)
        {
            return "Target CWE ID must be greater than zero.";
        }

        if (CurrentRule.SourceCweId == CurrentRule.TargetCweId)
        {
            return "Source and target CWE IDs must be different.";
        }

        if (CurrentRule.Score is < 0 or > 1000)
        {
            return "Score must be between 0 and 1000.";
        }

        if (string.IsNullOrWhiteSpace(CurrentRule.Rationale))
        {
            return "A rationale is required.";
        }

        if (string.IsNullOrWhiteSpace(CurrentRule.EvidenceReference))
        {
            return "An evidence reference is required.";
        }

        SemanticRule candidate = CurrentRule.ToSemanticRule();

        bool duplicateExists = Rules
            .Where((_, index) => index != selectedRuleIndex)
            .Any(existing =>
                existing.SourceCweId == candidate.SourceCweId &&
                existing.TargetCweId == candidate.TargetCweId &&
                existing.Relationship == candidate.Relationship);

        if (duplicateExists)
        {
            return "A rule with the same source, target, and relationship already exists.";
        }

        return null;
    }

    private int FindReferenceIndex(SemanticRule rule)
    {
        for (int index = 0; index < Rules.Count; index++)
        {
            /*
             * Records use value equality. ReferenceEquals ensures that editing
             * or deleting targets the exact selected collection item if two
             * records happen to contain identical values.
             */
            if (ReferenceEquals(Rules[index], rule))
            {
                return index;
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
}
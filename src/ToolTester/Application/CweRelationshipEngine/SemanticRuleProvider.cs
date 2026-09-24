using global::ToolTester.Application.Common.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ToolTester.Infrastructure.Services;

    public sealed class JsonSemanticRuleProvider
        : ISemanticRuleProvider
    {
        private readonly string _rulesFilePath;

        private readonly List<SemanticRule> _rules = [];

        public IReadOnlyCollection<SemanticRule> Rules =>
            _rules.AsReadOnly();

        public JsonSemanticRuleProvider()
        {
            _rulesFilePath = GetRulesFilePath();

            LoadRules();
        }

        public async Task ReloadAsync(
            CancellationToken cancellationToken = default)
        {
            _rules.Clear();

            if (!File.Exists(_rulesFilePath))
            {
                return;
            }

            await using var stream =
                File.OpenRead(_rulesFilePath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            options.Converters.Add(
                new JsonStringEnumConverter());

            var rules =
                await JsonSerializer.DeserializeAsync<
                    List<SemanticRule>>(
                    stream,
                    options,
                    cancellationToken);

            if (rules is not null)
            {
                _rules.AddRange(rules);
            }
        }

        private void LoadRules()
        {
            if (!File.Exists(_rulesFilePath))
            {
                return;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            options.Converters.Add(
                new JsonStringEnumConverter());

            var json =
                File.ReadAllText(_rulesFilePath);

            var rules =
                JsonSerializer.Deserialize<
                    List<SemanticRule>>(
                    json,
                    options);

            if (rules is not null)
            {
                _rules.AddRange(rules);
            }
        }

        private static string GetRulesFilePath()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments),
                "ToolTester");

            Directory.CreateDirectory(folder);

            return Path.Combine(
                folder,
                "semantic-rules.json");
        }
    }

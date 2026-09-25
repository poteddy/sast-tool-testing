using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ToolTester.Parsers.Checkmarx;

public sealed class CheckmarxResult
{
    [JsonProperty("type")] public string? Type { get; init; }
    [JsonProperty("label")] public string? Label { get; init; }
    [JsonProperty("id")] public string? Id { get; init; }
    [JsonProperty("similarityId")] public string? SimilarityId { get; init; }
    [JsonProperty("alternateId")] public string? AlternateId { get; init; }
    [JsonProperty("status")] public string? Status { get; init; }
    [JsonProperty("state")] public string? State { get; init; }
    [JsonProperty("severity")] public string? Severity { get; init; }
    [JsonProperty("created")] public DateTimeOffset? Created { get; init; }
    [JsonProperty("firstFoundAt")] public DateTimeOffset? FirstFoundAt { get; init; }
    [JsonProperty("foundAt")] public DateTimeOffset? FoundAt { get; init; }
    [JsonProperty("firstScanId")] public string? FirstScanId { get; init; }
    [JsonProperty("description")] public string? Description { get; init; }
    [JsonProperty("descriptionHTML")] public string? DescriptionHtml { get; init; }
    [JsonProperty("data")] public CheckmarxData? Data { get; init; }
    [JsonProperty("comments")] public JToken? Comments { get; init; }
    [JsonProperty("vulnerabilityDetails")] public VulnerabilityDetails? VulnerabilityDetails { get; init; }
}

public sealed class CheckmarxData
{
    [JsonProperty("queryId")] public string? QueryId { get; init; }
    [JsonProperty("queryName")] public string? QueryName { get; init; }
    [JsonProperty("group")] public string? Group { get; init; }
    [JsonProperty("resultHash")] public string? ResultHash { get; init; }
    [JsonProperty("languageName")] public string? LanguageName { get; init; }
    [JsonProperty("nodes")] public List<CheckmarxNode> Nodes { get; init; } = [];
}

public sealed record CheckmarxNode
{
    [JsonProperty("id")] public string? Id { get; init; }
    [JsonProperty("line")] public int? Line { get; init; }
    [JsonProperty("name")] public string? Name { get; init; }
    [JsonProperty("column")] public int? Column { get; init; }
    [JsonProperty("length")] public int? Length { get; init; }
    [JsonProperty("nodeID")] public string? NodeId { get; init; }
    [JsonProperty("fileName")] public string? FileName { get; init; }
    [JsonProperty("fullName")] public string? FullName { get; init; }
    [JsonProperty("methodLine")] public int? MethodLine { get; init; }
}

public sealed class VulnerabilityDetails
{
    [JsonProperty("cweId")] public int? CweId { get; init; }
    [JsonProperty("cvss")] public JToken? Cvss { get; init; }
    [JsonProperty("compliances")] public List<string> Compliances { get; init; } = [];
}

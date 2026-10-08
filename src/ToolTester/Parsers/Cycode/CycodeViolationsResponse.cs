using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace ToolTester.Parsers.Cycode;

public sealed class CycodeViolationsResponse
{
    [JsonProperty("items")]
    public List<CycodeViolation> Items { get; set; } = [];

    [JsonProperty("next_page_token")]
    public string? NextPageToken { get; set; }

    [JsonProperty("previous_page_token")]
    public string? PreviousPageToken { get; set; }

    [JsonProperty("total")]
    public int? Total { get; set; }

    [Newtonsoft.Json.JsonExtensionData]
    public IDictionary<string, JToken>? AdditionalData { get; set; }
}

public sealed class CycodeViolation
{
    [JsonProperty("source_policy_name")]
    public string? SourcePolicyName { get; set; }

    [JsonProperty("source_policy_type")]
    public string? SourcePolicyType { get; set; }

    [JsonProperty("source_entity_name")]
    public string? SourceEntityName { get; set; }

    [JsonProperty("source_entity_id")]
    public string? SourceEntityId { get; set; }

    [JsonProperty("detection_type_id")]
    public Guid? DetectionTypeId { get; set; }

    [JsonProperty("root_id")]
    public Guid? RootId { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("default_status_value")]
    public string? DefaultStatusValue { get; set; }

    [JsonProperty("status_updated_at")]
    public DateTimeOffset? StatusUpdatedAt { get; set; }

    [JsonProperty("first_closed_at")]
    public DateTimeOffset? FirstClosedAt { get; set; }

    [JsonProperty("last_detected_at")]
    public DateTimeOffset? LastDetectedAt { get; set; }

    [JsonProperty("status_reason")]
    public string? StatusReason { get; set; }

    [JsonProperty("status_change_message")]
    public string? StatusChangeMessage { get; set; }

    [JsonProperty("is_pending_review")]
    public bool IsPendingReview { get; set; }

    [JsonProperty("source_entity_type")]
    public string? SourceEntityType { get; set; }

    [JsonProperty("detection_details")]
    public CycodeDetectionDetails? DetectionDetails { get; set; }

    [JsonProperty("severity")]
    public string? Severity { get; set; }

    [JsonProperty("remediable")]
    public bool Remediable { get; set; }

    [JsonProperty("correlation_message")]
    public string? CorrelationMessage { get; set; }

    [JsonProperty("provider")]
    public string? Provider { get; set; }

    [JsonProperty("scan_id")]
    public Guid? ScanId { get; set; }

    [JsonProperty("scan_aggregation_id")]
    public Guid? ScanAggregationId { get; set; }

    [JsonProperty("assignee_id")]
    public Guid? AssigneeId { get; set; }

    [JsonProperty("status_updated_by_id")]
    public Guid? StatusUpdatedById { get; set; }

    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("is_hidden")]
    public bool IsHidden { get; set; }

    [JsonProperty("is_policy_disabled")]
    public bool IsPolicyDisabled { get; set; }

    [JsonProperty("tags")]
    public List<string> Tags { get; set; } = [];

    [JsonProperty("detection_rule_id")]
    public Guid? DetectionRuleId { get; set; }

    // The sample returns null and does not establish the non-null shape.
    [JsonProperty("classification")]
    public JToken? Classification { get; set; }

    [JsonProperty("priority")]
    public int Priority { get; set; }

    // JToken keeps this compatible if Cycode returns an object,
    // array, string, number, or null.
    [JsonProperty("metadata")]
    public JToken? Metadata { get; set; }

    [JsonProperty("labels")]
    public List<string> Labels { get; set; } = [];

    [JsonProperty("detection_id")]
    public string? DetectionId { get; set; }

    [JsonProperty("sdlc_stages")]
    public List<string> SdlcStages { get; set; } = [];

    [JsonProperty("policy_labels")]
    public List<string> PolicyLabels { get; set; } = [];

    [JsonProperty("category")]
    public string? Category { get; set; }

    [JsonProperty("sub_category")]
    public string? SubCategory { get; set; }

    [JsonProperty("sub_category_v2")]
    public string? SubCategoryV2 { get; set; }

    [JsonProperty("policy_tags")]
    public List<string> PolicyTags { get; set; } = [];

    [JsonProperty("remediations")]
    public List<JToken> Remediations { get; set; } = [];

    [JsonProperty("instruction_details")]
    public CycodeInstructionDetails? InstructionDetails { get; set; }

    [JsonProperty("external_detection_references")]
    public List<CycodeExternalDetectionReference> ExternalDetectionReferences
    {
        get;
        set;
    } = [];

    [JsonProperty("risk_score")]
    public double RiskScore { get; set; }

    [JsonProperty("risk_score_severity")]
    public string? RiskScoreSeverity { get; set; }

    [JsonProperty("sla_status")]
    public JToken? SlaStatus { get; set; }

    [JsonProperty("project_ids_str")]
    public List<string> ProjectIdsStr { get; set; } = [];

    [JsonProperty("project_ids")]
    public List<long> ProjectIds { get; set; } = [];

    [JsonProperty("tenant_id")]
    public Guid? TenantId { get; set; }

    [JsonProperty("id")]
    public Guid? Id { get; set; }

    [JsonProperty("created_date")]
    public DateTimeOffset CreatedDate { get; set; }

    [JsonProperty("updated_date")]
    public DateTimeOffset UpdatedDate { get; set; }

    [Newtonsoft.Json.JsonExtensionData]
    public IDictionary<string, JToken>? AdditionalData { get; set; }
}

public sealed class CycodeDetectionDetails
{
    [JsonProperty("organization_id")]
    public Guid? OrganizationId { get; set; }

    [JsonProperty("external_scanner_id")]
    public Guid? ExternalScannerId { get; set; }

    [JsonProperty("organization_name")]
    public string? OrganizationName { get; set; }

    [JsonProperty("policy_id")]
    public Guid? PolicyId { get; set; }

    [JsonProperty("detection_rule_id")]
    public Guid? DetectionRuleId { get; set; }

    [JsonProperty("file_path")]
    public string? FilePath { get; set; }

    [JsonProperty("file_name")]
    public string? FileName { get; set; }

    [JsonProperty("file_extension")]
    public string? FileExtension { get; set; }

    [JsonProperty("start_position")]
    public int? StartPosition { get; set; }

    [JsonProperty("end_position")]
    public int? EndPosition { get; set; }

    [JsonProperty("line")]
    public int? Line { get; set; }

    [JsonProperty("storage_details")]
    public CycodeStorageDetails? StorageDetails { get; set; }

    [JsonProperty("cwe")]
    public List<string> Cwe { get; set; } = [];

    [JsonProperty("owasp")]
    public List<string> Owasp { get; set; } = [];

    [JsonProperty("category")]
    public string? Category { get; set; }

    [JsonProperty("languages")]
    public List<string> Languages { get; set; } = [];

    [JsonProperty("repository_name")]
    public string? RepositoryName { get; set; }

    [JsonProperty("repository_id")]
    public string? RepositoryId { get; set; }

    [JsonProperty("branch_name")]
    public string? BranchName { get; set; }

    [JsonProperty("branch_id")]
    public string? BranchId { get; set; }

    [JsonProperty("commit_id")]
    public string? CommitId { get; set; }

    [JsonProperty("author_name")]
    public string? AuthorName { get; set; }

    [JsonProperty("author_email")]
    public string? AuthorEmail { get; set; }

    [JsonProperty("committer_name")]
    public string? CommitterName { get; set; }

    [JsonProperty("pull_request_number")]
    public long? PullRequestNumber { get; set; }

    [JsonProperty("line_in_file")]
    public int? LineInFile { get; set; }

    [JsonProperty("line_type")]
    public string? LineType { get; set; }

    [JsonProperty("entity_type")]
    public string? EntityType { get; set; }

    [JsonProperty("detection_uniqueness")]
    public string? DetectionUniqueness { get; set; }

    [JsonProperty("new_detection_uniqueness")]
    public string? NewDetectionUniqueness { get; set; }

    [JsonProperty("is_remediable")]
    public bool IsRemediable { get; set; }

    [JsonProperty("content_resolver")]
    public JToken? ContentResolver { get; set; }

    [JsonProperty("remediation_details")]
    public JToken? RemediationDetails { get; set; }

    [JsonProperty("remediation_actions")]
    public JToken? RemediationActions { get; set; }

    [JsonProperty("old_detection_id")]
    public string? OldDetectionId { get; set; }

    [JsonProperty("data_type")]
    public string? DataType { get; set; }

    [JsonProperty("data_category")]
    public string? DataCategory { get; set; }

    [JsonProperty("data_category_groups")]
    public List<string>? DataCategoryGroups { get; set; }

    [JsonProperty("scan_aggregation_id")]
    public Guid? ScanAggregationId { get; set; }

    [JsonProperty("external_severity")]
    public string? ExternalSeverity { get; set; }

    [JsonProperty("repository_url")]
    public string? RepositoryUrl { get; set; }

    [JsonProperty("project_id")]
    public string? ProjectId { get; set; }

    [JsonProperty("branch_url")]
    public string? BranchUrl { get; set; }

    [JsonProperty("file_url")]
    public string? FileUrl { get; set; }

    [Newtonsoft.Json.JsonExtensionData]
    public IDictionary<string, JToken>? AdditionalData { get; set; }
}

public sealed class CycodeStorageDetails
{
    [JsonProperty("path")]
    public string? Path { get; set; }

    [JsonProperty("folder")]
    public string? Folder { get; set; }

    [JsonProperty("size")]
    public long Size { get; set; }

    [JsonProperty("is_external")]
    public bool IsExternal { get; set; }

    [Newtonsoft.Json.JsonExtensionData]
    public IDictionary<string, JToken>? AdditionalData { get; set; }
}

public sealed class CycodeInstructionDetails
{
    [JsonProperty("instruction_name_to_single_id_map")]
    public Dictionary<string, JToken>? InstructionNameToSingleIdMap
    {
        get;
        set;
    }

    [JsonProperty("instruction_name_to_multiple_ids_map")]
    public Dictionary<string, List<JToken>>? InstructionNameToMultipleIdsMap
    {
        get;
        set;
    }

    [JsonProperty("instruction_tags")]
    public List<string>? InstructionTags { get; set; }

    [Newtonsoft.Json.JsonExtensionData]
    public IDictionary<string, JToken>? AdditionalData { get; set; }
}

public sealed class CycodeExternalDetectionReference
{
    [JsonProperty("external_detection_id")]
    public string? ExternalDetectionId { get; set; }

    [JsonProperty("external_integration_type")]
    public string? ExternalIntegrationType { get; set; }

    [JsonProperty("external_integration_id")]
    public string? ExternalIntegrationId { get; set; }

    [JsonProperty("external_integration_project_name")]
    public string? ExternalIntegrationProjectName { get; set; }

    [JsonProperty("external_integration_project_id")]
    public string? ExternalIntegrationProjectId { get; set; }

    [Newtonsoft.Json.JsonExtensionData]
    public IDictionary<string, JToken>? AdditionalData { get; set; }
}

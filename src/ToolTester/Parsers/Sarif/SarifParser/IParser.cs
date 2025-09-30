using Microsoft.CodeAnalysis.Sarif;
using ToolTester.Parsers.Sarif.Interfaces;

namespace ToolTester.Parsers.Sarif
{
    public interface IParser
    {
        static abstract string cve_try(string val);
        static abstract string GetMessageFromMultiformatMessageString<T>(T data, ReportingDescriptor rule);
        static abstract Dictionary<int, Dictionary<string, object>> get_artifacts(Run run);
        static abstract string get_description(Result result, ReportingDescriptor rule);
        static abstract CWEs get_item(Result result, List<ReportingDescriptor> rules, object artifacts, DateTime? run_date);
        static abstract string get_references(ReportingDescriptor rule);
        static abstract List<int> get_result_cwes_properties(Result result);
        static abstract List<ReportingDescriptor> get_rules(Run run);
        static abstract List<int> get_rule_cwes(ReportingDescriptor rule);
        static abstract TagsCollection get_rule_tags(ReportingDescriptor rule);
        static abstract string get_severity(Result result, ReportingDescriptor rule);
        static abstract ArtifactContent get_snippet(Result result);
        static abstract string get_title(Result result, ReportingDescriptor rule);
        static abstract List<int> search_cwe(string value, List<int> cwes);
        static abstract string Truncate(string value, int maxLength);
        object get_description_for_scan_types(object scan_type);
        List<CWEs> get_findings(Stream fs);
        object get_label_for_scan_types(object scan_type);
        object get_scan_types();
        List<CWEs> __get_items_from_run(Run run);
        DateTime? __get_last_invocation_date(Run data);
    }
}
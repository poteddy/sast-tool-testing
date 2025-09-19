using Microsoft.CodeAnalysis.Sarif;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using ToolTester.Parsers.Sarif.Interfaces;

namespace ToolTester.Parsers.Sarif
{
    public class Parser: object
    {
        public static string CWE_REGEX = @"(?i)cwe-\d+";

        public virtual object get_scan_types()
        {
            return new List<object> {
                "SARIF"
            };
        }

        public virtual object get_label_for_scan_types(object scan_type)
        {
            return scan_type;
        }

        public virtual object get_description_for_scan_types(object scan_type)
        {
            return "SARIF report file can be imported in SARIF format.";
        }

        // For simple interface of parser contract we just aggregate everything
        public virtual List<CWEs> get_findings(Stream fs)
        {

            using (StreamReader r = new StreamReader(fs))
            {

                string json = r.ReadToEnd();

                SarifLog tree = JsonConvert.DeserializeObject<SarifLog>(json);

                var items = new List<CWEs>();
                //  for each runs we just aggregate everything
                foreach (Run run in tree.Runs)
                {
                    items.AddRange(this.@__get_items_from_run(run));
                }
                return items;
            }
        }


        //public virtual object get_tests(object scan_type, object handle)
        //{
        //    var tree = json.load(handle);
        //    var tests = new List<object>();
        //    foreach (var run in tree.get("runs", new List<object>()))
        //    {
        //        var test = ParserTest(name: run["tool"]["driver"]["name"], type: run["tool"]["driver"]["name"], version: run["tool"]["driver"].get("version"));
        //        test.findings = this.@__get_items_from_run(run);
        //        tests.append(test);
        //    }
        //    return tests;
        //}

        public virtual List<CWEs> @__get_items_from_run(Run run)
        {
            var items = new List<CWEs>();
            // load rules
            var rules = get_rules(run);
            var artifacts = get_artifacts(run);
            // get the timestamp of the run if possible
            var run_date = this.@__get_last_invocation_date(run);
            foreach (var result in run.Results)
            {
                var item = get_item(result, rules, artifacts, run_date);
                if (item is not null)
                {
                    items.Add(item);
                }
            }
            return items;
        }

        public virtual DateTime? @__get_last_invocation_date(Run data)
        {
            var invocations = data.Invocations;
            if (invocations == null)
            {
                return null;
            }
            // try to get the last 'endTimeUtc'
            DateTime? raw_date = invocations[^1].EndTimeUtc;
            if (raw_date is null)
            {
                return null;
            }
            // if the data is here we try to convert it to datetime
            return raw_date;
        }


        public static List<ReportingDescriptor> get_rules(Run run)
        {
            var rules = new List<ReportingDescriptor>()
            {
            };
            foreach (var item in run.Tool.Driver.Rules)
            {
                rules.Add(item);
            }
            return rules;
        }

        public static TagsCollection get_rule_tags(ReportingDescriptor rule)
        {
            return rule.Tags;

        }

        public static List<int> search_cwe(string value, List<int> cwes)
        {
            var matches = Regex.Match(value, CWE_REGEX, RegexOptions.IgnoreCase);
            if (matches.Success)
            {
                cwes.Add(Convert.ToInt32(matches.Value.Split("-")[1]));
            }
            else
            {
                return null;
            }
            return cwes;
        }

        public static List<int> get_rule_cwes(ReportingDescriptor rule)
        {
            var cwes = new List<int>();
            // data of the specification
            if (rule.Relationships != null && rule.Relationships.Count > 0)
            {
                foreach (var relationship in rule.Relationships)
                {
                    var value = relationship.Target.Id;
                    search_cwe(value, cwes);
                }
                return cwes;
            }
            foreach (var tag in get_rule_tags(rule))
            {
                search_cwe(tag, cwes);
            }
            if (rule.PropertyNames.Contains("cwe"))
            {
                if (rule.GetProperty<SerializedPropertyInfo>("cwe").SerializedValue != null)
                {
                    var transformedvalue = rule.GetProperty<SerializedPropertyInfo>("cwe").SerializedValue;
                    search_cwe(transformedvalue, cwes);

                }

            }


            return cwes;
        }

        // Some tools like njsscan store the CWE in the properties of the result
        public static List<int> get_result_cwes_properties(Result result)
        {
            var cwes = new List<int>();
            if (result.RuleId != null)
            {

                var value = result.PropertyNames.FirstOrDefault("cwe");
                search_cwe(value, cwes);
            }
            return cwes;
        }

        public static Dictionary<int, Dictionary<string, object>> get_artifacts(Run run)
        {
            var artifacts = new Dictionary<int, Dictionary<string, object>>();
            int customIndex = 0; // hack because some tool doesn't generate this attribute

            if (run.Artifacts != null && run.Artifacts.Count > 0)
            {
                foreach (var treeArtifact in run.Artifacts)
                {
                    artifacts[treeArtifact.ParentIndex].Add(customIndex.ToString(), treeArtifact);
                    customIndex++;
                }
            }

            return artifacts;
        }


        // Get a message from multimessage struct
        // 
        //     See here for the specification: https://docs.oasis-open.org/sarif/sarif/v2.1.0/os/sarif-v2.1.0-os.html#_Toc34317468
        //     
        private static object GetPropValue(object src, string propName)
        {
            return src.GetType().GetProperty(propName).GetValue(src, null);
        }
        public static string GetMessageFromMultiformatMessageString<T>(T data, ReportingDescriptor rule)
        {
            if (rule != null && data.HasProperty("Id") && (GetPropValue(data, "Id") != null))
            {
                var messageStrings = rule.MessageStrings;
                string text = GetPropValue(messageStrings.Where(d => d.Key == GetPropValue(data, "id")), "text").ToString();
                var arguments = data.HasProperty("arguments") ? (List<object>)GetPropValue(data, "arguments") : new List<object>();

                // argument substitution
                for (int i = 0; i < 6; i++) // the specification limit to 6
                {
                    string substitutionStr = "{" + i + "}";
                    if (text.Contains(substitutionStr))
                    {
                        text = text.Replace(substitutionStr, arguments.Count > i ? arguments[i].ToString() : string.Empty);
                    }
                    else
                    {
                        return text;
                    }
                }
            }
            else
            {
                // TODO manage markdown
                return data.HasProperty("text") ? GetPropValue(data, "text").ToString() : string.Empty;
            }

            return string.Empty;
        }

        public static string cve_try(string val)
        {
            // Match only the first CVE!
            var cveSearch = Regex.Match(val, "(CVE-[0-9]+-[0-9]+)", RegexOptions.IgnoreCase);
            if (cveSearch.Success)
            {
                return cveSearch.Groups[1].Value.ToUpper();
            }
            else
            {
                return null;
            }
        }

        public static string get_title(Result result, ReportingDescriptor rule)
        {
            string title = null;
            if (result.Message != null)
            {
                title = GetMessageFromMultiformatMessageString(result.Message, rule);
            }
            if (string.IsNullOrEmpty(title) && rule is not null)
            {
                if (rule.ShortDescription != null)
                {
                    title = rule.ShortDescription.Text;
                }
                else if (rule.FullDescription != null)
                {
                    title = rule.FullDescription.Text;
                }
                else if (rule.Name != null)
                {
                    title = rule.Name;
                }
                else if (rule.Id != null)
                {
                    title = rule.Id;
                }
            }
            if (title is null)
            {
                throw new ArgumentException("not foud");
            }
            return Truncate(title, 150);
        }

        public static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        public static ArtifactContent get_snippet(Result result)
        {
            ArtifactContent snippet = null;
            if (result.Locations != null && result.Locations.Count > 0)
            {
                var location = result.Locations.FirstOrDefault();
                if (location.PhysicalLocation != null)
                {
                    if (location.PhysicalLocation.Region != null)
                    {
                        if (location.PhysicalLocation.Region.Snippet != null)
                        {
                            if (location.PhysicalLocation.Region.Snippet != null)
                            {
                                snippet = location.PhysicalLocation.Region.Snippet;
                            }
                        }
                    }
                    if (snippet is null && location.PhysicalLocation.ContextRegion != null)
                    {
                        {
                            if (location.PhysicalLocation.ContextRegion.Snippet != null)
                            {
                                if (location.PhysicalLocation.ContextRegion.Snippet != null)
                                {
                                    snippet = location.PhysicalLocation.ContextRegion.Snippet;
                                }
                            }
                        }
                    }
                }

            }
            return snippet;
        }

        public static string get_description(Result result, ReportingDescriptor rule)
        {
            var description = "";
            var message = "";
            if (result.Message != null)
            {
                message = result.Message.Text;
                description += string.Format("**Result message:** {0}\n", message);
            }
            if (get_snippet(result) is not null)
            {
                description += string.Format("**Snippet:**\n```{0}```\n", get_snippet(result));
            }
            if (rule is not null)
            {
                if (rule.Name != null)
                {
                    description += string.Format("**Rule name:** {0}\n", rule.Name);
                }
                var shortDescription = "";
                if (rule.ShortDescription != null)
                {
                    shortDescription = rule.ShortDescription.Text;
                    if (shortDescription != message)
                    {
                        description += string.Format("**Rule short description:** {0}\n", shortDescription);
                    }
                }
                if (rule.FullDescription != null)
                {
                    var fullDescription = rule.FullDescription.Text;
                    if (fullDescription != message && fullDescription != shortDescription)
                    {
                        description += string.Format("**Rule full description:** {0}\n", fullDescription);
                    }
                }
            }
            if (description.EndsWith("\n"))
            {
                description = description;
            }
            return description;
        }

        public static string get_references(ReportingDescriptor rule)
        {
            string reference = null;
            if (rule is not null)
            {
                if (rule.HelpUri != null)
                {
                    reference = rule.HelpUri.ToString();
                }
                else if (rule.Help != null)
                {
                    var helpText = rule.Help.Text;
                    if (helpText.StartsWith("http"))
                    {
                        reference = helpText;
                    }
                }
            }
            return reference;
        }

        public static string get_severity(Result result, ReportingDescriptor rule)
        {
            FailureLevel? severity = result.Level;
            if (severity is null && rule is not null)
            {
                // get the severity from the rule
                if (rule.DefaultConfiguration != null)
                {
                    severity = rule.DefaultConfiguration.Level;
                }
            }
            if (severity.Value == FailureLevel.Note)
            {
                return "Info";
            }
            else if (severity.Value == FailureLevel.Warning)
            {
                return "Medium";
            }
            else if (severity.Value == FailureLevel.Error)
            {
                return "Critical";
            }
            else
            {
                return "Medium";
            }
        }

        public static CWEs get_item(Result result, List<ReportingDescriptor> rules, object artifacts, DateTime? run_date)
        {
            // see https://docs.oasis-open.org/sarif/sarif/v2.1.0/csprd01/sarif-v2.1.0-csprd01.html / 3.27.9
            var kind = result.Kind;
            if (kind != ResultKind.Fail)
            {
                return null;
            }
            // if there is a location get it
            string file_path = null;
            int? line = null;
            if (result.Locations != null && result.Locations.Count > 0)
            {
                var location = result.Locations.FirstOrDefault();
                if (location.PhysicalLocation != null)
                {
                    file_path = location.PhysicalLocation.ArtifactLocation.Uri.ToString();
                    // 'region' attribute is optionnal
                    if (location.PhysicalLocation.Region != null)
                    {
                        line = location.PhysicalLocation.Region.StartLine;
                    }
                }
            }
            // test rule link
            var rule = rules.FirstOrDefault(d => d.Id == result.RuleId);
            var finding = new CWEs(title: get_title(result, rule), test: 3614, numericalSeverity: "100", foundBy: new List<int?>() { 1 }, severity: get_severity(result, rule), description: get_description(result, rule), staticFinding: true, dynamicFinding: false, filePath: file_path, line: line, references: get_references(rule));
            if (result.RuleId != null)
            {
                finding.VulnIdFromTool = result.RuleId.ToString();
                // for now we only support when the id of the rule is a CVE
                finding.Cve = cve_try(result.RuleId);
            }
            // some time the rule id is here but the tool doesn't define it
            if (rule is not null)
            {
                var cwes_extracted = get_rule_cwes(rule);
                if (cwes_extracted.Count > 0)
                {
                    finding.Cwe = cwes_extracted[^1];
                }
            }
            // manage the case that some tools produce CWE as properties of the result
            var cwes_properties_extracted = get_result_cwes_properties(result);
            if (cwes_properties_extracted.Count > 0)
            {
                finding.Cwe = cwes_properties_extracted[^1];
            }
            // manage fixes provided in the report
            if (result.Fixes != null && result.Fixes.Count > 0)
            {
                finding.Mitigation = string.Concat("", (from fix in result.Fixes
                                                        select fix.Description.Text).ToArray());
            }
            if (run_date != null)
            {
                finding.Date = run_date;
            }
            return finding;
        }
    }
    public static class Extensions
    {
        public static bool HasProperty(this object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName) != null;
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToolTester.Parsers.SemGrep
{

    public class Rootobject
    {
        public string version { get; set; }
        public Result[] results { get; set; }
        public Error[] errors { get; set; }
        public Paths paths { get; set; }
        public Time time { get; set; }
        public string engine_requested { get; set; }
        public object[] interfile_languages_used { get; set; }
        public object[] skipped_rules { get; set; }
    }

    public class Paths
    {
        public string[] scanned { get; set; }
    }

    public class Time
    {
        public object[] rules { get; set; }
        public float rules_parse_time { get; set; }
        public Profiling_Times profiling_times { get; set; }
        public Parsing_Time parsing_time { get; set; }
        public Scanning_Time scanning_time { get; set; }
        public Matching_Time matching_time { get; set; }
        public Tainting_Time tainting_time { get; set; }
        public object[] fixpoint_timeouts { get; set; }
        public Prefiltering prefiltering { get; set; }
        public object[] targets { get; set; }
        public int total_bytes { get; set; }
        public long max_memory_bytes { get; set; }
    }

    public class Profiling_Times
    {
        public float config_time { get; set; }
        public float core_time { get; set; }
        public float ignores_time { get; set; }
        public float total_time { get; set; }
    }

    public class Parsing_Time
    {
        public float total_time { get; set; }
        public Per_File_Time per_file_time { get; set; }
        public Very_Slow_Stats very_slow_stats { get; set; }
        public object[] very_slow_files { get; set; }
    }

    public class Per_File_Time
    {
        public float mean { get; set; }
        public float std_dev { get; set; }
    }

    public class Very_Slow_Stats
    {
        public float time_ratio { get; set; }
        public float count_ratio { get; set; }
    }

    public class Scanning_Time
    {
        public float total_time { get; set; }
        public Per_File_Time1 per_file_time { get; set; }
        public Very_Slow_Stats1 very_slow_stats { get; set; }
        public Very_Slow_Files[] very_slow_files { get; set; }
    }

    public class Per_File_Time1
    {
        public float mean { get; set; }
        public float std_dev { get; set; }
    }

    public class Very_Slow_Stats1
    {
        public float time_ratio { get; set; }
        public float count_ratio { get; set; }
    }

    public class Very_Slow_Files
    {
        public string fpath { get; set; }
        public float ftime { get; set; }
    }

    public class Matching_Time
    {
        public float total_time { get; set; }
        public Per_File_And_Rule_Time per_file_and_rule_time { get; set; }
        public Very_Slow_Stats2 very_slow_stats { get; set; }
        public object[] very_slow_rules_on_files { get; set; }
    }

    public class Per_File_And_Rule_Time
    {
        public float mean { get; set; }
        public float std_dev { get; set; }
    }

    public class Very_Slow_Stats2
    {
        public float time_ratio { get; set; }
        public float count_ratio { get; set; }
    }

    public class Tainting_Time
    {
        public float total_time { get; set; }
        public Per_Def_And_Rule_Time per_def_and_rule_time { get; set; }
        public Very_Slow_Stats3 very_slow_stats { get; set; }
        public object[] very_slow_rules_on_defs { get; set; }
    }

    public class Per_Def_And_Rule_Time
    {
        public float mean { get; set; }
        public float std_dev { get; set; }
    }

    public class Very_Slow_Stats3
    {
        public float time_ratio { get; set; }
        public float count_ratio { get; set; }
    }

    public class Prefiltering
    {
        public float project_level_time { get; set; }
        public float file_level_time { get; set; }
        public float rules_with_project_prefilters_ratio { get; set; }
        public float rules_with_file_prefilters_ratio { get; set; }
        public float rules_selected_ratio { get; set; }
        public float rules_matched_ratio { get; set; }
    }

    public class Result
    {
        public string check_id { get; set; }
        public string path { get; set; }
        public Start start { get; set; }
        public End end { get; set; }
        public Extra extra { get; set; }
    }

    public class Start
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class End
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class Extra
    {
        public Metavars metavars { get; set; }
        public string message { get; set; }
        public Metadata metadata { get; set; }
        public string severity { get; set; }
        public string fingerprint { get; set; }
        public string lines { get; set; }
        public bool is_ignored { get; set; }
        public string validation_state { get; set; }
        public Dataflow_Trace dataflow_trace { get; set; }
        public string engine_kind { get; set; }
    }

    public class Metavars
    {
        public RET_ARG1_ARG2 RET_ARG1_ARG2 { get; set; }
        public ARG1 ARG1 { get; set; }
        public STR STR { get; set; }
        public RETURN_TYPE RETURN_TYPE { get; set; }
        public FUNC FUNC { get; set; }
        public LOCAL_VAR_TYPE LOCAL_VAR_TYPE { get; set; }
        public LOCAL_VAR LOCAL_VAR { get; set; }
        public SINK_VAR SINK_VAR { get; set; }
        public TY TY { get; set; }
        public FUN FUN { get; set; }
        public PTR_TY PTR_TY { get; set; }
        public PTR_PARAM PTR_PARAM { get; set; }
        public _1 _1 { get; set; }
        public _4 _4 { get; set; }
        public SRC SRC { get; set; }
        public SCALAR_TYPE SCALAR_TYPE { get; set; }
        public LHS LHS { get; set; }
        public RHS RHS { get; set; }
        public SIZE SIZE { get; set; }
        public BUF BUF { get; set; }
        public VAR VAR { get; set; }
        public SYSTEM SYSTEM { get; set; }
        public FMT_STRING FMT_STRING { get; set; }
    }

    public class RET_ARG1_ARG2
    {
        public Start1 start { get; set; }
        public End1 end { get; set; }
        public string abstract_content { get; set; }
    }

    public class Start1
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class End1
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class ARG1
    {
        public Start2 start { get; set; }
        public End2 end { get; set; }
        public string abstract_content { get; set; }
        public Propagated_Value propagated_value { get; set; }
    }

    public class Start2
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class End2
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class Propagated_Value
    {
        public Svalue_Start svalue_start { get; set; }
        public Svalue_End svalue_end { get; set; }
        public string svalue_abstract_content { get; set; }
    }

    public class Svalue_Start
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class Svalue_End
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class STR
    {
        public Start3 start { get; set; }
        public End3 end { get; set; }
        public string abstract_content { get; set; }
    }

    public class Start3
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class End3
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class RETURN_TYPE
    {
        public Start4 start { get; set; }
        public End4 end { get; set; }
        public string abstract_content { get; set; }
    }

    public class Start4
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class End4
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class FUNC
    {
        public Start5 start { get; set; }
        public End5 end { get; set; }
        public string abstract_content { get; set; }
    }

    public class Start5
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class End5
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class LOCAL_VAR_TYPE
    {
        public Start6 start { get; set; }
        public End6 end { get; set; }
        public string abstract_content { get; set; }
    }

    public class Start6
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class End6
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class LOCAL_VAR
    {
        public Start7 start { get; set; }
        public End7 end { get; set; }
        public string abstract_content { get; set; }
        public Propagated_Value1 propagated_value { get; set; }
    }

    public class Start7
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class End7
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class Propagated_Value1
    {
        public Svalue_Start1 svalue_start { get; set; }
        public Svalue_End1 svalue_end { get; set; }
        public string svalue_abstract_content { get; set; }
    }

    public class Svalue_Start1
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class Svalue_End1
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class SINK_VAR
    {
        public Start8 start { get; set; }
        public End8 end { get; set; }
        public string abstract_content { get; set; }
        public Propagated_Value2 propagated_value { get; set; }
    }

    public class Start8
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class End8
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class Propagated_Value2
    {
        public Svalue_Start2 svalue_start { get; set; }
        public Svalue_End2 svalue_end { get; set; }
        public string svalue_abstract_content { get; set; }
    }

    public class Svalue_Start2
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class Svalue_End2
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class TY
    {
        public Start9 start { get; set; }
        public End9 end { get; set; }
        public string abstract_content { get; set; }
    }

    public class Start9
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class End9
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class FUN
    {
        public Start10 start { get; set; }
        public End10 end { get; set; }
        public string abstract_content { get; set; }
    }

    public class Start10
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class End10
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class PTR_TY
    {
        public Start11 start { get; set; }
        public End11 end { get; set; }
        public string abstract_content { get; set; }
    }

    public class Start11
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class End11
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class PTR_PARAM
    {
        public Start12 start { get; set; }
        public End12 end { get; set; }
        public string abstract_content { get; set; }
        public Propagated_Value3 propagated_value { get; set; }
    }

    public class Start12
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class End12
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class Propagated_Value3
    {
        public Svalue_Start3 svalue_start { get; set; }
        public Svalue_End3 svalue_end { get; set; }
        public string svalue_abstract_content { get; set; }
    }

    public class Svalue_Start3
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class Svalue_End3
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class _1
    {
        public Start13 start { get; set; }
        public End13 end { get; set; }
        public string abstract_content { get; set; }
    }

    public class Start13
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class End13
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class _4
    {
        public Start14 start { get; set; }
        public End14 end { get; set; }
        public string abstract_content { get; set; }
    }

    public class Start14
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class End14
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class SRC
    {
        public Start15 start { get; set; }
        public End15 end { get; set; }
        public string abstract_content { get; set; }
        public Propagated_Value4 propagated_value { get; set; }
    }

    public class Start15
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class End15
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class Propagated_Value4
    {
        public Svalue_Start4 svalue_start { get; set; }
        public Svalue_End4 svalue_end { get; set; }
        public string svalue_abstract_content { get; set; }
    }

    public class Svalue_Start4
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class Svalue_End4
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class SCALAR_TYPE
    {
        public Start16 start { get; set; }
        public End16 end { get; set; }
        public string abstract_content { get; set; }
    }

    public class Start16
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class End16
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class LHS
    {
        public Start17 start { get; set; }
        public End17 end { get; set; }
        public string abstract_content { get; set; }
        public Propagated_Value5 propagated_value { get; set; }
    }

    public class Start17
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class End17
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class Propagated_Value5
    {
        public Svalue_Start5 svalue_start { get; set; }
        public Svalue_End5 svalue_end { get; set; }
        public string svalue_abstract_content { get; set; }
    }

    public class Svalue_Start5
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class Svalue_End5
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class RHS
    {
        public Start18 start { get; set; }
        public End18 end { get; set; }
        public string abstract_content { get; set; }
        public Propagated_Value6 propagated_value { get; set; }
    }

    public class Start18
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class End18
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class Propagated_Value6
    {
        public Svalue_Start6 svalue_start { get; set; }
        public Svalue_End6 svalue_end { get; set; }
        public string svalue_abstract_content { get; set; }
    }

    public class Svalue_Start6
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class Svalue_End6
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class SIZE
    {
        public Start19 start { get; set; }
        public End19 end { get; set; }
        public string abstract_content { get; set; }
    }

    public class Start19
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class End19
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class BUF
    {
        public Start20 start { get; set; }
        public End20 end { get; set; }
        public string abstract_content { get; set; }
    }

    public class Start20
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class End20
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class VAR
    {
        public Start21 start { get; set; }
        public End21 end { get; set; }
        public string abstract_content { get; set; }
        public Propagated_Value7 propagated_value { get; set; }
    }

    public class Start21
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class End21
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class Propagated_Value7
    {
        public Svalue_Start7 svalue_start { get; set; }
        public Svalue_End7 svalue_end { get; set; }
        public string svalue_abstract_content { get; set; }
    }

    public class Svalue_Start7
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class Svalue_End7
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class SYSTEM
    {
        public Start22 start { get; set; }
        public End22 end { get; set; }
        public string abstract_content { get; set; }
    }

    public class Start22
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class End22
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class FMT_STRING
    {
        public Start23 start { get; set; }
        public End23 end { get; set; }
        public string abstract_content { get; set; }
    }

    public class Start23
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class End23
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class Metadata
    {
        public string likelihood { get; set; }
        public string impact { get; set; }
        public string confidence { get; set; }
        public string category { get; set; }
        public string[] subcategory { get; set; }
        public string[] cert { get; set; }
        public string[] cwe { get; set; }
        public bool cwe2020top25 { get; set; }
        public bool cwe2021top25 { get; set; }
        public bool cwe2022top25 { get; set; }
        public string displayname { get; set; }
        public string[] functionalcategories { get; set; }
        public string[] references { get; set; }
        public string[] technology { get; set; }
        public string license { get; set; }
        public string[] vulnerability_class { get; set; }
        public string source { get; set; }
        public string shortlink { get; set; }
        public SemgrepDev semgrepdev { get; set; }
        public string[] supersedes { get; set; }
        public bool interfile { get; set; }
        public string[] owasp { get; set; }
    }

    public class SemgrepDev
    {
        public Rule rule { get; set; }
    }

    public class Rule
    {
        public string origin { get; set; }
        public int r_id { get; set; }
        public string rule_id { get; set; }
        public int rv_id { get; set; }
        public string url { get; set; }
        public string version_id { get; set; }
    }

    public class Dataflow_Trace
    {
        public object[] taint_source { get; set; }
        public Intermediate_Vars[] intermediate_vars { get; set; }
        public object[] taint_sink { get; set; }
    }

    public class Intermediate_Vars
    {
        public Location location { get; set; }
        public string content { get; set; }
    }

    public class Location
    {
        public string path { get; set; }
        public Start24 start { get; set; }
        public End24 end { get; set; }
    }

    public class Start24
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class End24
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class Error
    {
        public int code { get; set; }
        public string level { get; set; }
        public object type { get; set; }
        public string message { get; set; }
        public string path { get; set; }
        public string rule_id { get; set; }
        public Span[] spans { get; set; }
    }

    public class Span
    {
        public string file { get; set; }
        public Start25 start { get; set; }
        public End25 end { get; set; }
    }

    public class Start25
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

    public class End25
    {
        public int line { get; set; }
        public int col { get; set; }
        public int offset { get; set; }
    }

}

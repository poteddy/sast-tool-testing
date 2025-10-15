using Newtonsoft.Json;
using System.Data;
using System.Text.RegularExpressions;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Common.Models;

namespace ToolTester.Parsers.SemGrep
{
    public class Parser : IParser
    {
        public static string CWE_REGEX = @"(?i)cwe-\d+";
        private bool disposedValue;

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
        public async virtual Task<List<CWEs>> Get_findings(Stream fs)
        {

            using (StreamReader r = new StreamReader(fs))
            {

                string json = await r.ReadToEndAsync();

                Rootobject tree = JsonConvert.DeserializeObject<Rootobject>(json);

                var items = new List<CWEs>();
                //  for each runs we just aggregate everything
                foreach (var run in tree.results)
                {
                    var file_path = run.path;
                    var extra = run.extra;
                    var metadata = extra.metadata;
                    var stringcwe = metadata.cwe;
                    string cwe = stringcwe[0];
                    
                   var finding = new CWEs(title: cwe, test: 3614, numericalSeverity: "100", foundBy: new List<int?>() { 1 }, severity: extra.severity, description: extra.message, staticFinding: true, dynamicFinding: false, filePath: file_path, line: run.start.line, references: extra.metadata.references.FirstOrDefault());
                    Regex regex = new Regex(@"-(?<number>\d+):");
                    Match match = regex.Match(cwe);
                    if (match.Success)
                    {
                        var gmath = match.Groups["number"].Value;
                        finding.Cwe = int.Parse(gmath);
                    }
                    
                    items.Add(finding);

                }
                return items;
            }
        }

        List<int> IParser.Get_findings(Stream fs)
        {
            throw new NotImplementedException();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Parser()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

}

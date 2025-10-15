using Microsoft.CodeAnalysis.Sarif;
using System.Text.RegularExpressions;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Common.Models;

namespace ToolTester.Parsers.Veracode
{
    public class Parser : IParser
    {
        private bool disposedValue;

        public async virtual Task<List<CWEs>> Get_findings(Stream fs)
        {
            var items = new List<CWEs>();
            detailedreport detailreport = await XMLExtensions.ReadXMLAsync(fs);
            foreach (var item in detailreport.severity)
            {
                if (item == null) continue;
                if (item.category == null) continue;
                foreach (var cat in item.category)
                {
                    
                    foreach (var cwe in cat.cwe)
                    {
                        foreach(var staticflaws in cwe.staticflaws)
                        {
                            var finding = new CWEs(title: cwe.cwename, test: 3614, numericalSeverity: "100", foundBy: new List<int?>() { 1 }, severity: item.level.ToString(), description: cwe.description.text.ToString(), staticFinding: true, dynamicFinding: false, filePath: staticflaws.sourcefilepath, line: staticflaws.line, references: "");

                            finding.Cwe = cwe.cweid;

                            items.Add(finding);
                        }
                      
                    }

                }

            }
            return items;

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

        List<int> IParser.Get_findings(Stream fs)
        {
            throw new NotImplementedException();
        }
    }
}

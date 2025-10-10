using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ToolTester.Application.Common.Interfaces;

namespace ToolTester.Infrastructure.Services
{
    public  class ZipfileService : IZipfileService
    {
        public  Dictionary<int,int> FileCount(string zipFileName)
        {
            using (ZipArchive archive = ZipFile.Open(zipFileName, ZipArchiveMode.Read))
            {
                Dictionary<int, int> folders = new Dictionary<int, int>();

                // We count only named (i.e. that are with files) entries
                foreach (var entry in archive.Entries)
                {
                    if (!entry.FullName.EndsWith("/"))
                    {
                        var rg = @"(testcases/)(CWE(?=\D*)(\d+))";

                        var m = Regex.Match(entry.FullName, rg);
                        if (m.Success)
                        {
                            var cwe = int.Parse(m.Groups[3].Value);
                            if (folders.ContainsKey(cwe))
                            {
                                folders[cwe] += 1;
                            }
                            else
                            {
                                folders.Add(cwe, 1);
                            }
                                
                        }
                    }
                }

                return folders;
            }
        }

   
    }
}

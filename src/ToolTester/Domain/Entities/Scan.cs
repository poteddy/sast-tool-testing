using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolTester.Domain.Common;

namespace ToolTester.Domain.Entities
{
    public class Scan : IEntity
    {
        public int Id { get; set; }

        public int ToolId { get; set; }
        public Tool Tool { get; set; }

      
        public List<CWETestResult> TestResults { get; set; }
    }
    public class Tool :IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Format {  get; set; }

    }

}

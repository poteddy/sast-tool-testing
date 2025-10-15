using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToolTester.Presentation.Models
{
    public class MyButtonDataItem
    {
        public string ButtonText { get; set; }
        public object CommandParameter { get; set; } // Optional, for passing data to the command
    }
}

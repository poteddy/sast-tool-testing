using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolTester.Application.Common.Interfaces;

namespace ToolTester.Application.Common.Models
{

    public class SyncfusionSetting : ISyncfusionSetting
    {
        public string Registration_Key { get; set; } = string.Empty;
    }
    public class JulietProjectSetting : IJulietProjectSetting
    {
        public string Path { get; set; } = string.Empty;
    }
    public class MitreCatalogSetting : IMitreCatalogSetting
    {
        public string Path { get; set; } = string.Empty;
    }
}

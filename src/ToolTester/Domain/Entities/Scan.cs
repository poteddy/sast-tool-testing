using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolTester.Domain.Common;

namespace ToolTester.Domain.Entities;

public sealed class Scan : IEntity
{
    public int Id { get; set; }

    public int ToolId { get; set; }

    public Tool Tool { get; set; } = null!;

    public List<CWETestResult> TestResults { get; set; } =
        [];

    public List<Report> Reports { get; set; } =
        [];
}

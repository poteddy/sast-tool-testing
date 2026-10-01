using ToolTester.Domain.Common;

namespace ToolTester.Domain.Entities;

public sealed class Tool : IEntity
{
    public int Id { get; set; }

    public string Name { get; set; } =
        string.Empty;

    public string Format { get; set; } =
        string.Empty;

    public List<Scan> Scans { get; set; } =
        [];

    public List<Report> Reports { get; set; } =
        [];
}
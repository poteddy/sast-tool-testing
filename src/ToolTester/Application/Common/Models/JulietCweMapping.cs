namespace ToolTester.Application.Common.Models;

public sealed record JulietCweMapping(
    int PrimaryCweId,
    int SecondaryCweId,
    string FileName);
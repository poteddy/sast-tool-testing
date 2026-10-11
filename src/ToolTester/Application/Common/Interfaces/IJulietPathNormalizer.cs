using System;
using System.Collections.Generic;
using System.Text;

namespace ToolTester.Application.Common.Interfaces;


public interface IJulietPathNormalizer
{
    string Normalize(string? filePath);

    string GetFileName(string? filePath);

    int? ExtractCweId(string? filePath);

    bool IsSupportedSourceFile(string? filePath);
}
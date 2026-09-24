using System;
using System.Collections.Generic;
using System.Text;

namespace ToolTester.Application.CweRelationshipEngine
{
    public static class CweAbstractionParser
    {
        public static CweAbstractionLevel Parse(
            string? value)
        {
            return Enum.TryParse<CweAbstractionLevel>(
                value,
                ignoreCase: true,
                out var abstraction)
                    ? abstraction
                    : CweAbstractionLevel.Unknown;
        }
    }
}

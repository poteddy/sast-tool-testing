using System;
using System.Collections.Generic;
using System.Text;

namespace ToolTester.Application.Common.Interfaces;

public interface ICweRootCauseResolver
{
    Task<int?> ResolveRootCauseAsync(
        int scannerCweId,
        int? groundTruthCweId = null,
        CancellationToken cancellationToken = default);
}
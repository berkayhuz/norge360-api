// <copyright file="GatewayDiagnosticsMeter.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Diagnostics.Metrics;

namespace Norge360.ApiGateway.Observability;

internal static class GatewayDiagnosticsMeter
{
    public static readonly Meter Instance = new("Norge360.ApiGateway.Requests");
}

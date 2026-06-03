// <copyright file="InternalServiceRequestSigner.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Community.Infrastructure.Services;

public interface IInternalServiceRequestSigner
{
    ValueTask SignAsync(HttpRequestMessage request, CancellationToken cancellationToken);
}

internal sealed class NoOpInternalServiceRequestSigner : IInternalServiceRequestSigner
{
    public ValueTask SignAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}

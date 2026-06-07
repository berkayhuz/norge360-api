// <copyright file="ICommunityNotificationTargetService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Contracts.Requests;
using Norge360.Accounts.Contracts.Responses;

namespace Norge360.Accounts.Application.Abstractions;

public interface ICommunityNotificationTargetService
{
    Task<CommunityNotificationTargetsResponse> ResolveAsync(
        CommunityNotificationTargetsRequest request,
        CancellationToken cancellationToken = default);
}

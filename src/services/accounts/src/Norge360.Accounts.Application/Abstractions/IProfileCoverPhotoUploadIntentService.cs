// <copyright file="IProfileCoverPhotoUploadIntentService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Application.Models;
using Norge360.Accounts.Contracts.Requests;

namespace Norge360.Accounts.Application.Abstractions;

public interface IProfileCoverPhotoUploadIntentService
{
    Task<CreateAvatarUploadIntentResult> CreateAsync(
        Guid authUserId,
        CreateAvatarUploadIntentRequest request,
        CancellationToken cancellationToken = default);
}

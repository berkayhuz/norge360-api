// <copyright file="UpdateMyProfileRequest.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Accounts.Contracts.Requests;

public sealed record UpdateMyProfileRequest(
    string? DisplayName,
    string? Bio,
    string? Country,
    string? City,
    string? District,
    string? Occupation,
    string? Company,
    string? Website,
    string? ProfileVisibility,
    string? CommentAudience,
    bool? HideLikeCounts);

// <copyright file="UsernameAvailabilityResponse.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Accounts.Contracts.Responses;

public sealed record UsernameAvailabilityResponse(
    string Username,
    string NormalizedUsername,
    bool IsAvailable,
    string? Reason,
    string? SuggestedUsername);

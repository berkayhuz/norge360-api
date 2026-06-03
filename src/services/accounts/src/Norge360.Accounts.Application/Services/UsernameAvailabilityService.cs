// <copyright file="UsernameAvailabilityService.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Contracts.Responses;

namespace Norge360.Accounts.Application.Services;

public sealed class UsernameAvailabilityService(
    IUsernameNormalizer normalizer,
    IUsernameValidator validator,
    IUserProfileRepository userProfileRepository,
    IReservedUsernameRepository reservedUsernameRepository,
    IUsernameHistoryRepository usernameHistoryRepository) : IUsernameAvailabilityService
{
    public async Task<UsernameAvailabilityResponse> CheckAsync(
        string? username,
        Guid? excludingProfileId = null,
        CancellationToken cancellationToken = default)
    {
        var value = username?.Trim() ?? string.Empty;
        var normalizedUsername = normalizer.Normalize(value);
        var validation = validator.Validate(value);

        if (!validation.IsValid)
        {
            return Unavailable(value, normalizedUsername, validation.Reason);
        }

        if (await reservedUsernameRepository.IsReservedAsync(normalizedUsername, cancellationToken))
        {
            return Unavailable(value, normalizedUsername, "username_reserved");
        }

        if (await userProfileRepository.ExistsByNormalizedUsernameAsync(
                normalizedUsername,
                excludingProfileId,
                cancellationToken))
        {
            return Unavailable(value, normalizedUsername, "username_in_use");
        }

        if (await usernameHistoryRepository.IsLockedByAnotherProfileAsync(
                normalizedUsername,
                excludingProfileId,
                cancellationToken))
        {
            return Unavailable(value, normalizedUsername, "username_locked");
        }

        return new UsernameAvailabilityResponse(
            value,
            normalizedUsername,
            IsAvailable: true,
            Reason: null,
            SuggestedUsername: null);
    }

    private static UsernameAvailabilityResponse Unavailable(
        string username,
        string normalizedUsername,
        string? reason) =>
        new(
            username,
            normalizedUsername,
            IsAvailable: false,
            Reason: reason,
            SuggestedUsername: null);
}

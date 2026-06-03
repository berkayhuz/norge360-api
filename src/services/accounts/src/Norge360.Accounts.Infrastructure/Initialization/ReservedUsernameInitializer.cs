// <copyright file="ReservedUsernameInitializer.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Infrastructure.Options;
using Norge360.Accounts.Infrastructure.Persistence;
using Norge360.Clock;

namespace Norge360.Accounts.Infrastructure.Initialization;

public sealed class ReservedUsernameInitializer(
    AccountsDbContext dbContext,
    IUsernameNormalizer usernameNormalizer,
    IOptions<ReservedUsernameSeedOptions> options,
    IClock clock) : IReservedUsernameInitializer
{
    private const string SeedActor = "system:seed";
    private const int MaximumNormalizedValueLength = 100;
    private const int MaximumReasonLength = 256;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var entries = NormalizeEntries(options.Value.ReservedUsernames);
        if (entries.Count == 0)
        {
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        foreach (var entry in entries)
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "ReservedUsernames"
                    ("Id", "NormalizedValue", "Reason", "IsActive", "CreatedAt", "CreatedBy")
                VALUES
                    ({Guid.NewGuid()}, {entry.NormalizedValue}, {entry.Reason}, TRUE, {clock.UtcNow}, {SeedActor})
                ON CONFLICT ("NormalizedValue") WHERE "IsActive" = TRUE
                DO UPDATE SET
                    "Reason" = EXCLUDED."Reason",
                    "IsActive" = TRUE;
                """,
                cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private IReadOnlyCollection<NormalizedSeedEntry> NormalizeEntries(
        IEnumerable<ReservedUsernameSeedEntry> entries)
    {
        var normalized = new Dictionary<string, NormalizedSeedEntry>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            var normalizedValue = usernameNormalizer.Normalize(entry.Value);
            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                continue;
            }

            if (normalizedValue.Length > MaximumNormalizedValueLength)
            {
                throw new InvalidOperationException(
                    $"Reserved username '{normalizedValue}' exceeds the {MaximumNormalizedValueLength}-character storage limit.");
            }

            var reason = CleanOrNull(entry.Reason);
            if (reason?.Length > MaximumReasonLength)
            {
                throw new InvalidOperationException(
                    $"Reserved username reason for '{normalizedValue}' exceeds the {MaximumReasonLength}-character storage limit.");
            }

            normalized[normalizedValue] = new NormalizedSeedEntry(normalizedValue, reason);
        }

        return normalized.Values
            .OrderBy(entry => entry.NormalizedValue, StringComparer.Ordinal)
            .ToArray();
    }

    private static string? CleanOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

    private sealed record NormalizedSeedEntry(string NormalizedValue, string? Reason);
}

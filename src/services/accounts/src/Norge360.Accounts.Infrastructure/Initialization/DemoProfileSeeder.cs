// <copyright file="DemoProfileSeeder.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Norge360.Accounts.Domain.Entities;
using Norge360.Accounts.Domain.Enums;
using Norge360.Accounts.Infrastructure.Persistence;
using Norge360.Clock;

namespace Norge360.Accounts.Infrastructure.Initialization;

public sealed class DemoProfileSeeder(AccountsDbContext dbContext, IClock clock)
{
    private const string SeedActor = "system:seed";

    private static readonly IReadOnlyCollection<DemoProfile> Profiles =
    [
        new("11111111-1111-1111-1111-111111111111", "aurora", "Aurora Demir", "Istanbul'dan gunluk sehir notlari.", true),
        new("22222222-2222-2222-2222-222222222222", "deniz", "Deniz Arslan", "Mahalle kesifleri ve ulasim ipuclari.", false),
        new("33333333-3333-3333-3333-333333333333", "emre", "Emre Kaya", "Kisa topluluk guncellemeleri paylasiyorum.", false),
        new("44444444-4444-4444-4444-444444444444", "selin", "Selin Yildiz", "Kafeler, parklar ve sakin rotalar.", true),
        new("55555555-5555-5555-5555-555555555555", "kerem", "Kerem Aydin", "Haftalik sehir yasami gozlemleri.", false),
        new("66666666-6666-6666-6666-666666666666", "elif", "Elif Cetin", "Etkinlikler ve yeni mekanlar hakkinda notlar.", false),
        new("77777777-7777-7777-7777-777777777777", "can", "Can Koc", "Kisa rehberler ve pratik oneriler.", true),
        new("88888888-8888-8888-8888-888888888888", "yaren", "Yaren Gunes", "Komsuluk ve gunluk hayat paylasimlari.", false),
        new("99999999-9999-9999-9999-999999999999", "baris", "Baris Uslu", "Yerel kesifler ve yararli baglantilar.", false),
        new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "zeynep", "Zeynep Oz", "Sehirin iyi taraflarini bir araya getiriyorum.", true)
    ];

    public async Task SeedDemoProfilesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        foreach (var profileSeed in Profiles)
        {
            var authUserId = Guid.Parse(profileSeed.AuthUserId);
            var profile = await dbContext.UserProfiles.IgnoreQueryFilters().FirstOrDefaultAsync(
                x => x.AuthUserId == authUserId || x.Username == profileSeed.Username,
                cancellationToken);

            if (profile is not null)
            {
                profile.AuthUserId = authUserId;
                profile.Username = profileSeed.Username;
                profile.NormalizedUsername = NormalizeUsername(profileSeed.Username);
                profile.DisplayName = profileSeed.DisplayName;
                profile.Bio = profileSeed.Bio;
                profile.IsVerified = profileSeed.IsVerified;
                profile.ProfileVisibility = ProfileVisibility.Public;
                profile.AccountType = AccountType.Personal;
                profile.IsDeleted = false;
                profile.Activate();
                profile.UpdatedAt = clock.UtcNow.UtcDateTime;
                profile.UpdatedBy = SeedActor;
                continue;
            }

            dbContext.UserProfiles.Add(new UserProfile
            {
                AuthUserId = authUserId,
                Username = profileSeed.Username,
                NormalizedUsername = NormalizeUsername(profileSeed.Username),
                DisplayName = profileSeed.DisplayName,
                Bio = profileSeed.Bio,
                IsVerified = profileSeed.IsVerified,
                ProfileVisibility = ProfileVisibility.Public,
                AccountType = AccountType.Personal,
                FollowersCount = 0,
                FollowingCount = 0,
                PostsCount = 0,
                CreatedAt = clock.UtcNow.UtcDateTime,
                CreatedBy = SeedActor
            });
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static string NormalizeUsername(string username) => username.Trim().ToUpperInvariant();

    private sealed record DemoProfile(string AuthUserId, string Username, string DisplayName, string Bio, bool IsVerified);
}

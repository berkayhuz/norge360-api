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
        new("11111111-1111-1111-1111-111111111111", "sigrid", "Sigrid Nilsen", "Skriver om fjellturer, byliv og kystkafeer.", true, 18400, 620, 340),
        new("22222222-2222-2222-2222-222222222222", "eirik", "Eirik Solberg", "Deler korte glimt fra Oslo og fjellet.", false, 16750, 540, 210),
        new("33333333-3333-3333-3333-333333333333", "marta", "Marta Hovland", "Mat, natur og små lokale favoritter.", false, 15120, 490, 275),
        new("44444444-4444-4444-4444-444444444444", "olav", "Olav Berg", "Enkle råd om turer, trening og hverdagsliv.", true, 14300, 610, 198),
        new("55555555-5555-5555-5555-555555555555", "elin", "Elin Aas", "Skriver om design, kaffe og rolige helger.", false, 13240, 430, 180),
        new("66666666-6666-6666-6666-666666666666", "jonas", "Jonas Dahl", "Fanger hverdagsøyeblikk fra kysten.", false, 12890, 410, 226),
        new("77777777-7777-7777-7777-777777777777", "nora", "Nora Lie", "Korte notater om kultur og byliv.", true, 12110, 390, 160),
        new("88888888-8888-8888-8888-888888888888", "ivar", "Ivar Haugen", "Turstier, vinterlys og gode utsikter.", false, 11870, 370, 204),
        new("99999999-9999-9999-9999-999999999999", "karoline", "Karoline Myhre", "Lokale anbefalinger og små byvandringer.", false, 11220, 350, 145),
        new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "magnus", "Magnus Haug", "Deler favoritter fra fjell, sjø og by.", true, 10840, 330, 188),
        new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", "silje", "Silje Vik", "Fine steder for kaffe, bøker og samtaler.", false, 10360, 310, 171),
        new("cccccccc-cccc-cccc-cccc-cccccccccccc", "haakon", "Håkon Nygård", "Fotograferer nordlys, havn og gamle gater.", false, 9980, 295, 149),
        new("dddddddd-dddd-dddd-dddd-dddddddddddd", "liv", "Liv Sand", "Rolige tips om natur, mat og nabolag.", true, 9620, 280, 132),
        new("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee", "tor", "Tor Eide", "Hverdagstanker fra turstier og sentrum.", false, 9280, 266, 221),
        new("ffffffff-ffff-ffff-ffff-ffffffffffff", "ida", "Ida Strand", "Små historier fra kysten og byen.", false, 9030, 250, 164),
        new("12121212-1212-1212-1212-121212121212", "leif", "Leif Moen", "Deler lokale favoritter og gode utsikter.", true, 8740, 245, 141),
        new("13131313-1313-1313-1313-131313131313", "inger", "Inger Holm", "Kultur, nabolag og stille søndager.", false, 8460, 238, 156),
        new("14141414-1414-1414-1414-141414141414", "vetle", "Vetle Solheim", "Turer, regn og glimt av sol mellom fjellene.", false, 8210, 228, 187),
        new("15151515-1515-1515-1515-151515151515", "ragnhild", "Ragnhild Skaar", "Små observasjoner fra hverdagen i nord.", true, 7980, 220, 133),
        new("16161616-1616-1616-1616-161616161616", "daniel", "Daniel Bergset", "Byliv, badstue og raske kaffestopp.", false, 7740, 215, 172),
        new("17171717-1717-1717-1717-171717171717", "thea", "Thea Lunde", "Skriver om konserter, bøker og gode samtaler.", false, 7510, 210, 146),
        new("18181818-1818-1818-1818-181818181818", "adrian", "Adrian Fosse", "Lager små guider til parker og utsiktspunkter.", true, 7320, 204, 129),
        new("19191919-1919-1919-1919-191919191919", "malin", "Malin Aune", "Kombinerer naturbilder med urbane detaljer.", false, 7090, 198, 158),
        new("1a1a1a1a-1a1a-1a1a-1a1a-1a1a1a1a1a1a", "fredrik", "Fredrik Løkken", "Fjell, fjord og små historier fra veien.", false, 6880, 194, 175),
        new("1b1b1b1b-1b1b-1b1b-1b1b-1b1b1b1b1b1b", "anita", "Anita Hegge", "Deler tips om kafeer, markeder og roligere gater.", true, 6640, 188, 124),
        new("1c1c1c1c-1c1c-1c1c-1c1c-1c1c1c1c1c1c", "stian", "Stian Egeland", "Korte oppdateringer fra hverdagsliv og reiser.", false, 6410, 182, 163),
        new("1d1d1d1d-1d1d-1d1d-1d1d-1d1d1d1d1d1d", "randi", "Randi Kittilsen", "Leter etter gode bakker, utsikter og utsøkt kaffe.", false, 6180, 176, 137),
        new("1e1e1e1e-1e1e-1e1e-1e1e-1e1e1e1e1e1e", "kristoffer", "Kristoffer Aasheim", "Skriver om lokalmat, markeder og små byopplevelser.", true, 5940, 171, 151),
        new("1f1f1f1f-1f1f-1f1f-1f1f-1f1f1f1f1f1f", "helga", "Helga Drønen", "Klar for fjell, hav og stille morgener.", false, 5730, 166, 119),
        new("20202020-2020-2020-2020-202020202020", "per", "Per Myklebust", "Enkel livsstil, turstøvler og kaffe på termos.", false, 5510, 160, 184),
        new("21212121-2121-2121-2121-212121212121", "mikkel", "Mikkel Volden", "Deler bilder fra kysten, skogen og byen.", true, 5290, 154, 128),
        new("23232323-2323-2323-2323-232323232323", "ingrid", "Ingrid Brekke", "Små anbefalinger fra steder jeg liker å gå.", false, 5060, 148, 139),
        new("24242424-2424-2424-2424-242424242424", "rune", "Rune Tveit", "Turer, værskifter og kaffe med utsikt.", false, 4830, 142, 167),
        new("25252525-2525-2525-2525-252525252525", "alva", "Alva Kleveland", "Hverdagsnotater med fokus på ro og natur.", true, 4620, 136, 123),
        new("26262626-2626-2626-2626-262626262626", "lasse", "Lasse Nystad", "Liker smale gater, gode bakere og sjøkanten.", false, 4410, 132, 150),
        new("27272727-2727-2727-2727-272727272727", "elise", "Elise Rønning", "Skriver om små fellesturer og fine utsikter.", false, 4200, 128, 111),
        new("28282828-2828-2828-2828-282828282828", "gjermund", "Gjermund Solås", "Kulturspor, turstier og lokalhistorie.", true, 3990, 124, 145),
        new("29292929-2929-2929-2929-292929292929", "stine", "Stine Lien", "Tips til rolige steder, lys og varme drikker.", false, 3780, 120, 104),
        new("2a2a2a2a-2a2a-2a2a-2a2a-2a2a2a2a2a2a", "rolf", "Rolf Vangen", "Deler små historier fra fjord og fjell.", false, 3570, 116, 159),
        new("2b2b2b2b-2b2b-2b2b-2b2b-2b2b2b2b2b2b", "elinor", "Elinor Sunde", "Kaffe, kultur og korte turer gjennom byen.", true, 3360, 112, 121),
        new("2c2c2c2c-2c2c-2c2c-2c2c-2c2c2c2c2c2c", "odd", "Odd Kårstad", "Liker vinterlys, fjellstier og stille vann.", false, 3140, 108, 134),
        new("2d2d2d2d-2d2d-2d2d-2d2d-2d2d2d2d2d2d", "maren", "Maren Eik", "Lager små guider til nabolag og kyststeder.", false, 2930, 104, 97),
        new("2e2e2e2e-2e2e-2e2e-2e2e-2e2e2e2e2e2e", "sondre", "Sondre Aukrust", "Fanger hverdagsscener fra tog, torg og tur.", true, 2720, 100, 142),
        new("2f2f2f2f-2f2f-2f2f-2f2f-2f2f2f2f2f2f", "kaja", "Kaja Dalen", "Rolige bilder av natur, lys og byliv.", false, 2510, 96, 118),
        new("30303030-3030-3030-3030-303030303030", "arild", "Arild Tangen", "Korte notater om musikk, kaffe og utsikt.", false, 2310, 92, 109),
        new("31313131-3131-3131-3131-313131313131", "tiril", "Tiril Huse", "Utforsker parker, broer og stille gater.", true, 2140, 88, 101),
        new("32323232-3232-3232-3232-323232323232", "jørgen", "Jørgen Næss", "Deler små reiseminner og lokale steder.", false, 1980, 84, 136)
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
                profile.FollowersCount = profileSeed.FollowersCount;
                profile.FollowingCount = profileSeed.FollowingCount;
                profile.PostsCount = profileSeed.PostsCount;
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
                FollowersCount = profileSeed.FollowersCount,
                FollowingCount = profileSeed.FollowingCount,
                PostsCount = profileSeed.PostsCount,
                ProfileVisibility = ProfileVisibility.Public,
                AccountType = AccountType.Personal,
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

    private sealed record DemoProfile(
        string AuthUserId,
        string Username,
        string DisplayName,
        string Bio,
        bool IsVerified,
        int FollowersCount,
        int FollowingCount,
        int PostsCount);
}

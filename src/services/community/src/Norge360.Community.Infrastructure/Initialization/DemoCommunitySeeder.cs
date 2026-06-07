// <copyright file="DemoCommunitySeeder.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Norge360.Clock;
using Norge360.Community.Domain.Entities;
using Norge360.Community.Domain.Enums;
using Norge360.Community.Domain.Utilities;
using Norge360.Community.Infrastructure.Persistence;

namespace Norge360.Community.Infrastructure.Initialization;

public sealed class DemoCommunitySeeder(CommunityDbContext dbContext, IClock clock)
{
    private const string SeedActor = "system:seed";

    private static readonly IReadOnlyList<DemoPost> Posts =
    [
        new("11111111-1111-1111-1111-111111111111", "Jeg prøvde den nyåpnede strandpromenaden på morgenturen min. Det er ekstra fint i de stille timene.", "Oslo", "Sentrum", ["https://picsum.photos/seed/norge360-aurora-1/1200/800"], "Seed-bilde av Aurora på tur langs vannet."),
        new("22222222-2222-2222-2222-222222222222", "I dag var kollektivtrafikken full, men jeg sparte likevel 15 minutter med en alternativ rute.", "Bergen", "Bergenhus", ["https://picsum.photos/seed/norge360-deniz-2/1200/800"], "Seed-bilde av Deniz sitt pendlerinnlegg."),
        new("33333333-3333-3333-3333-333333333333", "Jeg stakk innom den lille bokhandelen i nabolaget, og de ansatte var veldig hjelpsomme. Slike steder gjør byen hyggeligere.", "Trondheim", "Midtbyen", ["https://picsum.photos/seed/norge360-emre-3/1200/800"], "Seed-bilde av Emre sitt bokhandelsbesøk."),
        new("44444444-4444-4444-4444-444444444444", "Mitt kveldstips: en kort spasertur i parken og en kaffe ved vannet etterpå.", "Stavanger", "Eiganes og Våland", ["https://picsum.photos/seed/norge360-selin-4/1200/800"], "Seed-bilde av Selin sin kveldstur."),
        new("55555555-5555-5555-5555-555555555555", "Hvis noen har samlet info om den nye sykkelveien i fellesskapet, kan dere gjerne legge igjen en kommentar.", "Oslo", "Grünerløkka", ["https://picsum.photos/seed/norge360-kerem-5a/1200/800", "https://picsum.photos/seed/norge360-kerem-5b/1200/800"], "Seed-bilde av Kerem sitt innlegg om sykkelveien."),
        new("66666666-6666-6666-6666-666666666666", "Prisene på nabolagsmarkedet var litt mer stabile denne helgen enn forrige uke.", "Bergen", "Årstad", ["https://picsum.photos/seed/norge360-elif-6/1200/800"], "Seed-bilde av Elif sin markedsrapport."),
        new("77777777-7777-7777-7777-777777777777", "Jeg fant en ny kafé. Arbeidsområdet var stille, og Wi-Fi-en var rask nok.", "Trondheim", "Lerkendal", ["https://picsum.photos/seed/norge360-can-7/1200/800"], "Seed-bilde av Can sin kaféoppdagelse."),
        new("88888888-8888-8888-8888-888888888888", "Meldingen om en bortkommen katt spredte seg raskt i nabolagsgruppen. Samholdet fungerer virkelig.", "Tromsø", "Tromsdalen", ["https://picsum.photos/seed/norge360-yaren-8/1200/800"], "Seed-bilde av Yaren sitt kattevarsel."),
        new("99999999-9999-9999-9999-999999999999", "På kveldene var kollektivtrafikken roligere. Bare et lite tips til dere som planlegger daglige reiseruter.", "Stavanger", "Madla", ["https://picsum.photos/seed/norge360-baris-9/1200/800"], "Seed-bilde av Baris sin kollektivtrafikknote."),
        new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "En av tingene som gjør byen bedre, er grøntområder man kan nå med en kort spasertur.", "Tromsø", "Kvaløysletta", ["https://picsum.photos/seed/norge360-zeynep-10/1200/800"], "Seed-bilde av Zeynep sin note om grøntområder."),
        new("11111111-1111-1111-1111-111111111111", "Et litt lengre innlegg: I morges gikk jeg en lang rute langs kysten, tok med kaffe og senket tempoet mens jeg så lyset speile seg i sjøen. Noen ganger handler en god morgen i byen mer om å finne ett stille øyeblikk enn om å rekke alt mulig. Da jeg kom hjem, merket jeg også at jeg klarte å styre energien bedre resten av dagen.", "Oslo", "Frogner", ["https://picsum.photos/seed/norge360-aurora-long-11a/1200/800", "https://picsum.photos/seed/norge360-aurora-long-11b/1200/800"], "Seed-bilde av Aurora sitt lange innlegg."),
        new("44444444-4444-4444-4444-444444444444", "Etter en hektisk uke lagde jeg en roligere kveldsplan for meg selv: sitte litt i parken, høre på omgivelsene uten hodetelefoner og slå av telefonvarsler. Selv om det er en kort pause, kan slike øyeblikk endre rytmen for hele uka. Hvis du har en lignende rutine, er jeg nysgjerrig på hvilke tidspunkter som fungerer best for deg.", "Bergen", "Laksevåg", ["https://picsum.photos/seed/norge360-selin-long-12a/1200/800", "https://picsum.photos/seed/norge360-selin-long-12b/1200/800", "https://picsum.photos/seed/norge360-selin-long-12c/1200/800"], "Seed-bilde av Selin sitt lange innlegg.")
    ];

    public async Task SeedDemoPostsAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        var existingDemoPosts = await dbContext.CommunityPosts
            .IgnoreQueryFilters()
            .Include(x => x.Media)
            .Where(x => x.CreatedBy == SeedActor)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        for (var index = 0; index < Posts.Count; index++)
        {
            var postSeed = Posts[index];
            var authorId = Guid.Parse(postSeed.UserId);
            var existingPost = index < existingDemoPosts.Count ? existingDemoPosts[index] : null;

            if (existingPost is not null)
            {
                await SyncDemoPostAsync(existingPost, postSeed, cancellationToken);
                continue;
            }

            var post = new CommunityPost
            {
                UserId = authorId,
                Slug = PublicSlugGenerator.CreateNumericSlug(),
                Caption = postSeed.Caption,
                City = postSeed.City,
                District = postSeed.District,
                Status = CommunityPostStatus.Published,
                CreatedAt = clock.UtcNow.UtcDateTime.AddMinutes(-(Posts.Count - index)),
                CreatedBy = SeedActor
            };

            dbContext.CommunityPosts.Add(post);
            await dbContext.SaveChangesAsync(cancellationToken);
            await EnsureDemoMediaAsync(authorId, postSeed, cancellationToken);
        }

        for (var index = Posts.Count; index < existingDemoPosts.Count; index++)
        {
            var post = existingDemoPosts[index];
            post.IsDeleted = true;
            post.DeletedAt = clock.UtcNow.UtcDateTime;
            post.UpdatedBy = SeedActor;

            foreach (var media in post.Media)
            {
                media.IsDeleted = true;
                media.DeletedAt = clock.UtcNow.UtcDateTime;
                media.UpdatedBy = SeedActor;
            }
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureDemoMediaAsync(Guid authorId, DemoPost postSeed, CancellationToken cancellationToken)
    {
        var post = await dbContext.CommunityPosts.IgnoreQueryFilters().FirstOrDefaultAsync(
            x => x.UserId == authorId && x.Caption == postSeed.Caption,
            cancellationToken);

        if (post is null)
        {
            return;
        }

        var existingMediaCount = await dbContext.CommunityPostMedia.IgnoreQueryFilters().CountAsync(
            x => x.PostId == post.Id,
            cancellationToken);

        for (var index = existingMediaCount; index < postSeed.MediaUrls.Count; index++)
        {
            dbContext.CommunityPostMedia.Add(new CommunityPostMedia
            {
                PostId = post.Id,
                StorageKey = $"seed/community/posts/{post.Id:N}-{index}.jpg",
                PublicUrl = postSeed.MediaUrls[index],
                ContentType = "image/jpeg",
                SizeBytes = 184_320,
                Width = 1200,
                Height = 800,
                Order = (short)index,
                Status = CommunityMediaStatus.Ready,
                CreatedAt = clock.UtcNow.UtcDateTime,
                CreatedBy = SeedActor
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SyncDemoPostAsync(CommunityPost post, DemoPost postSeed, CancellationToken cancellationToken)
    {
        post.UserId = Guid.Parse(postSeed.UserId);
        post.Slug = string.IsNullOrWhiteSpace(post.Slug) ? PublicSlugGenerator.CreateNumericSlug() : post.Slug;
        post.Caption = postSeed.Caption;
        post.City = postSeed.City;
        post.District = postSeed.District;
        post.Status = CommunityPostStatus.Published;
        post.UpdatedBy = SeedActor;

        var existingMedia = post.Media
            .OrderBy(x => x.Order)
            .ToList();

        for (var index = 0; index < postSeed.MediaUrls.Count; index++)
        {
            var mediaUrl = postSeed.MediaUrls[index];
            var media = index < existingMedia.Count ? existingMedia[index] : null;

            if (media is null)
            {
                dbContext.CommunityPostMedia.Add(new CommunityPostMedia
                {
                    Post = post,
                    StorageKey = $"seed/community/posts/{post.Id:N}-{index}.jpg",
                    PublicUrl = mediaUrl,
                    ContentType = "image/jpeg",
                    SizeBytes = 184_320,
                    Width = 1200,
                    Height = 800,
                    Order = (short)index,
                    Status = CommunityMediaStatus.Ready,
                    CreatedAt = clock.UtcNow.UtcDateTime,
                    CreatedBy = SeedActor
                });
                continue;
            }

            media.StorageKey = $"seed/community/posts/{post.Id:N}-{index}.jpg";
            media.PublicUrl = mediaUrl;
            media.ContentType = "image/jpeg";
            media.SizeBytes = 184_320;
            media.Width = 1200;
            media.Height = 800;
            media.Order = (short)index;
            media.Status = CommunityMediaStatus.Ready;
            media.UpdatedBy = SeedActor;
            media.IsDeleted = false;
            media.DeletedAt = null;
        }

        for (var index = postSeed.MediaUrls.Count; index < existingMedia.Count; index++)
        {
            var media = existingMedia[index];
            media.IsDeleted = true;
            media.DeletedAt = clock.UtcNow.UtcDateTime;
            media.UpdatedBy = SeedActor;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record DemoPost(string UserId, string Caption, string City, string District, IReadOnlyList<string> MediaUrls, string AltText);
}

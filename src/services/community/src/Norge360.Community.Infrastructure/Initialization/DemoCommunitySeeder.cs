// <copyright file="DemoCommunitySeeder.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Norge360.Clock;
using Norge360.Community.Domain.Entities;
using Norge360.Community.Domain.Enums;
using Norge360.Community.Infrastructure.Persistence;

namespace Norge360.Community.Infrastructure.Initialization;

public sealed class DemoCommunitySeeder(CommunityDbContext dbContext, IClock clock)
{
    private const string SeedActor = "system:seed";

    private static readonly IReadOnlyList<DemoPost> Posts =
    [
        new("11111111-1111-1111-1111-111111111111", "Sabah yuruyusumde yeni acilan sahil yolunu denedim. Sessiz saatlerde cok keyifli.", "Oslo", "Sentrum", ["https://picsum.photos/seed/norge360-aurora-1/1200/800"], "Seed image for Aurora's waterfront walk."),
        new("22222222-2222-2222-2222-222222222222", "Bugun toplu tasima kalabalikti ama alternatif rota sayesinde 15 dakika kazandim.", "Bergen", "Bergenhus", ["https://picsum.photos/seed/norge360-deniz-2/1200/800"], "Seed image for Deniz's commute note."),
        new("33333333-3333-3333-3333-333333333333", "Mahalledeki kucuk kitapciya ugradim, calisanlar cok ilgiliydi. Boyle yerler sehri guzel yapiyor.", "Trondheim", "Midtbyen", ["https://picsum.photos/seed/norge360-emre-3/1200/800"], "Seed image for Emre's bookstore visit."),
        new("44444444-4444-4444-4444-444444444444", "Aksam icin onerim: parkta kisa bir yuruyus ve ardindan sahil kahvesi iyi geliyor.", "Stavanger", "Eiganes og Valand", ["https://picsum.photos/seed/norge360-selin-4/1200/800"], "Seed image for Selin's evening walk."),
        new("55555555-5555-5555-5555-555555555555", "Toplulukta yeni acilan bisiklet yolu hakkinda bilgi toplayan varsa yorum birakabilir.", "Oslo", "Grunerlokka", ["https://picsum.photos/seed/norge360-kerem-5a/1200/800", "https://picsum.photos/seed/norge360-kerem-5b/1200/800"], "Seed image for Kerem's bicycle lane post."),
        new("66666666-6666-6666-6666-666666666666", "Bu hafta sonu semt pazarinda fiyatlar gecen haftaya gore biraz daha dengeliydi.", "Bergen", "Arstad", ["https://picsum.photos/seed/norge360-elif-6/1200/800"], "Seed image for Elif's market report."),
        new("77777777-7777-7777-7777-777777777777", "Yeni bir kafe kesfettim. Calisma alani sessiz ve Wi-Fi yeterince hizliydi.", "Trondheim", "Lerkendal", ["https://picsum.photos/seed/norge360-can-7/1200/800"], "Seed image for Can's cafe discovery."),
        new("88888888-8888-8888-8888-888888888888", "Mahalle grubunda kaybolan kedi ilani hizla yayildi; dayanisma gercekten ise yariyor.", "Tromso", "Tromsdalen", ["https://picsum.photos/seed/norge360-yaren-8/1200/800"], "Seed image for Yaren's cat notice."),
        new("99999999-9999-9999-9999-999999999999", "Aksam saatlerinde toplu tasima daha sakindi. Gunluk yol plani yapanlar icin kucuk bir not.", "Stavanger", "Madla", ["https://picsum.photos/seed/norge360-baris-9/1200/800"], "Seed image for Baris's transit note."),
        new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "Sehirde yasam kalitesini artiran seylerden biri de kisa yuruyusle ulasilabilen yesil alanlar.", "Tromso", "Kvaloysletta", ["https://picsum.photos/seed/norge360-zeynep-10/1200/800"], "Seed image for Zeynep's green space note."),
        new("11111111-1111-1111-1111-111111111111", "Uzun bir not birakiyorum: bu sabah sahilde uzun bir rota yurudum, yanima kahve aldim ve gun isiginin denizin uzerindeki yansimasini izleyerek biraz yavasladim. Bazen sehirde iyi bir sabah, plan yapmaktan cok tek bir sakin an yakalamakla ilgili oluyor. Eve donunce de gun boyunca enerjimi daha iyi yonetebildigimi fark ettim.", "Oslo", "Frogner", ["https://picsum.photos/seed/norge360-aurora-long-11a/1200/800", "https://picsum.photos/seed/norge360-aurora-long-11b/1200/800"], "Seed image for Aurora's long-form post."),
        new("44444444-4444-4444-4444-444444444444", "Gecen hafta yogun tempodan sonra aksam icin kendime daha sakin bir plan yaptim: parkta biraz oturmak, kulakligi takmadan cevreyi dinlemek ve telefon bildirimlerini kapatmak. Kisa gorunse de bu tur mola anlari bazen tum haftanin ritmini degistiriyor. Siz de benzer bir rutin kuruyorsaniz merak ediyorum, hangi saatler sizde daha iyi calisiyor?", "Bergen", "Laksevag", ["https://picsum.photos/seed/norge360-selin-long-12a/1200/800", "https://picsum.photos/seed/norge360-selin-long-12b/1200/800", "https://picsum.photos/seed/norge360-selin-long-12c/1200/800"], "Seed image for Selin's long-form post.")
    ];

    public async Task SeedDemoPostsAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        for (var index = 0; index < Posts.Count; index++)
        {
            var postSeed = Posts[index];
            var authorId = Guid.Parse(postSeed.UserId);
            var exists = await dbContext.CommunityPosts.IgnoreQueryFilters().AnyAsync(
                x => x.UserId == authorId && x.Caption == postSeed.Caption,
                cancellationToken);

            if (exists)
            {
                await EnsureDemoMediaAsync(authorId, postSeed, cancellationToken);
                continue;
            }

            var post = new CommunityPost
            {
                UserId = authorId,
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

    private sealed record DemoPost(string UserId, string Caption, string City, string District, IReadOnlyList<string> MediaUrls, string AltText);
}

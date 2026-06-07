using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Norge360.Community.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillCommunityCommentCreatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                WITH ranked AS (
                    SELECT
                        c."Id",
                        COALESCE(
                            NULLIF(c."UpdatedAt", TIMESTAMPTZ '0001-01-01 00:00:00+00'),
                            p."CreatedAt"
                        ) AS fallback_created_at,
                        row_number() OVER (PARTITION BY c."PostId" ORDER BY c."Id") AS row_num
                    FROM "CommunityComments" c
                    INNER JOIN "CommunityPosts" p ON p."Id" = c."PostId"
                    WHERE c."CreatedAt" = TIMESTAMPTZ '0001-01-01 00:00:00+00'
                )
                UPDATE "CommunityComments" c
                SET "CreatedAt" = ranked.fallback_created_at + make_interval(mins => ranked.row_num)
                FROM ranked
                WHERE c."Id" = ranked."Id";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}

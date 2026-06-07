using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Norge360.Community.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityPublicSlugs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "CommunityPosts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "CommunityComments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql("""
                WITH seed AS (
                    SELECT (1000000000000000000 + floor(random() * 8000000000000000000))::bigint AS base_value
                ),
                ordered AS (
                    SELECT "Id", (SELECT base_value FROM seed) + row_number() OVER (ORDER BY "CreatedAt", "Id") AS slug_value
                    FROM "CommunityPosts"
                )
                UPDATE "CommunityPosts" p
                SET "Slug" = ordered.slug_value::text
                FROM ordered
                WHERE p."Id" = ordered."Id";
                """);

            migrationBuilder.Sql("""
                WITH seed AS (
                    SELECT (1000000000000000000 + floor(random() * 8000000000000000000))::bigint AS base_value
                ),
                ordered AS (
                    SELECT "Id", (SELECT base_value FROM seed) + row_number() OVER (ORDER BY "CreatedAt", "Id") AS slug_value
                    FROM "CommunityComments"
                )
                UPDATE "CommunityComments" c
                SET "Slug" = ordered.slug_value::text
                FROM ordered
                WHERE c."Id" = ordered."Id";
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "CommunityPosts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "CommunityComments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunityPosts_Slug",
                table: "CommunityPosts",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunityComments_Slug",
                table: "CommunityComments",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CommunityPosts_Slug",
                table: "CommunityPosts");

            migrationBuilder.DropIndex(
                name: "IX_CommunityComments_Slug",
                table: "CommunityComments");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "CommunityPosts");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "CommunityComments");
        }
    }
}

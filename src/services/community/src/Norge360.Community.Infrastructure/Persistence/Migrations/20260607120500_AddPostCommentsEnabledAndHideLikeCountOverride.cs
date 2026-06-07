using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Norge360.Community.Infrastructure.Persistence.Migrations;

public partial class AddPostCommentsEnabledAndHideLikeCountOverride : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "CommentsEnabled",
            table: "CommunityPosts",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<bool>(
            name: "HideLikeCountOverride",
            table: "CommunityPosts",
            type: "boolean",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CommentsEnabled",
            table: "CommunityPosts");

        migrationBuilder.DropColumn(
            name: "HideLikeCountOverride",
            table: "CommunityPosts");
    }
}

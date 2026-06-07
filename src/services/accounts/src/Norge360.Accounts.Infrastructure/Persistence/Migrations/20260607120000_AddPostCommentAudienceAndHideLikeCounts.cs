using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Norge360.Accounts.Infrastructure.Persistence.Migrations;

public partial class AddPostCommentAudienceAndHideLikeCounts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<short>(
            name: "CommentAudience",
            table: "UserProfiles",
            type: "smallint",
            nullable: false,
            defaultValue: (short)0);

        migrationBuilder.AddColumn<bool>(
            name: "HideLikeCounts",
            table: "UserProfiles",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CommentAudience",
            table: "UserProfiles");

        migrationBuilder.DropColumn(
            name: "HideLikeCounts",
            table: "UserProfiles");
    }
}

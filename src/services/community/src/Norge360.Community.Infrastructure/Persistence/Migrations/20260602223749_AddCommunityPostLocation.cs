using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Norge360.Community.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityPostLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "CommunityPosts",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "CommunityPosts",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "CommunityPosts");

            migrationBuilder.DropColumn(
                name: "District",
                table: "CommunityPosts");
        }
    }
}

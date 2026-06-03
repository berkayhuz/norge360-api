using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Norge360.Discovery.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscoverySnapshotEngagementCounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FollowersCount",
                table: "DiscoverySubjectSnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PostsCount",
                table: "DiscoverySubjectSnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FollowersCount",
                table: "DiscoverySubjectSnapshots");

            migrationBuilder.DropColumn(
                name: "PostsCount",
                table: "DiscoverySubjectSnapshots");
        }
    }
}

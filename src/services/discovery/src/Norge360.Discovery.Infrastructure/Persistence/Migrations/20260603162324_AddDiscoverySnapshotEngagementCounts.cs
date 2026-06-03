// <copyright file="20260603162324_AddDiscoverySnapshotEngagementCounts.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

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

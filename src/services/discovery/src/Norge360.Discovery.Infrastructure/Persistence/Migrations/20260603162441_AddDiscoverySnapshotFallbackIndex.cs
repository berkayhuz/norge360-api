// <copyright file="20260603162441_AddDiscoverySnapshotFallbackIndex.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Norge360.Discovery.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscoverySnapshotFallbackIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DiscoverySubjectSnapshots_SubjectType_IsActive_IsDeleted_V~1",
                table: "DiscoverySubjectSnapshots",
                columns: new[] { "SubjectType", "IsActive", "IsDeleted", "Visibility", "FollowersCount", "PostsCount" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DiscoverySubjectSnapshots_SubjectType_IsActive_IsDeleted_V~1",
                table: "DiscoverySubjectSnapshots");
        }
    }
}

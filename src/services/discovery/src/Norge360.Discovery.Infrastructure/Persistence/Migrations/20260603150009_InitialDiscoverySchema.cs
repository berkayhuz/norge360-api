// <copyright file="20260603150009_InitialDiscoverySchema.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Norge360.Discovery.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialDiscoverySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscoveryDailyAggregates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<short>(type: "smallint", nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    FollowPoints = table.Column<int>(type: "integer", nullable: false),
                    ProfileViewPoints = table.Column<int>(type: "integer", nullable: false),
                    LikePoints = table.Column<int>(type: "integer", nullable: false),
                    CommentPoints = table.Column<int>(type: "integer", nullable: false),
                    NegativePoints = table.Column<int>(type: "integer", nullable: false),
                    RawScore = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscoveryDailyAggregates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiscoveryEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<short>(type: "smallint", nullable: false),
                    SourceService = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SourceEntityType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SourceEntityId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetEntityType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    TargetEntityId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    DeduplicationKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsValid = table.Column<bool>(type: "boolean", nullable: false),
                    InvalidReason = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscoveryEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiscoveryRankings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RankingType = table.Column<short>(type: "smallint", nullable: false),
                    TargetType = table.Column<short>(type: "smallint", nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    WindowStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WindowEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscoveryRankings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiscoverySubjectSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectType = table.Column<short>(type: "smallint", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Username = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Bio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    Visibility = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscoverySubjectSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveryDailyAggregates_Date",
                table: "DiscoveryDailyAggregates",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveryDailyAggregates_TargetType_TargetId_Date",
                table: "DiscoveryDailyAggregates",
                columns: new[] { "TargetType", "TargetId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveryEvents_DeduplicationKey",
                table: "DiscoveryEvents",
                column: "DeduplicationKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveryEvents_EventType_OccurredAt",
                table: "DiscoveryEvents",
                columns: new[] { "EventType", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveryEvents_SourceEntityType_SourceEntityId_ActorUserId",
                table: "DiscoveryEvents",
                columns: new[] { "SourceEntityType", "SourceEntityId", "ActorUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveryEvents_TargetProfileId_OccurredAt",
                table: "DiscoveryEvents",
                columns: new[] { "TargetProfileId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveryRankings_RankingType_Rank",
                table: "DiscoveryRankings",
                columns: new[] { "RankingType", "Rank" });

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveryRankings_Score",
                table: "DiscoveryRankings",
                column: "Score");

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveryRankings_TargetType_TargetId",
                table: "DiscoveryRankings",
                columns: new[] { "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_DiscoverySubjectSnapshots_SubjectType_IsActive_IsDeleted_Vi~",
                table: "DiscoverySubjectSnapshots",
                columns: new[] { "SubjectType", "IsActive", "IsDeleted", "Visibility" });

            migrationBuilder.CreateIndex(
                name: "IX_DiscoverySubjectSnapshots_SubjectType_SubjectId",
                table: "DiscoverySubjectSnapshots",
                columns: new[] { "SubjectType", "SubjectId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscoveryDailyAggregates");

            migrationBuilder.DropTable(
                name: "DiscoveryEvents");

            migrationBuilder.DropTable(
                name: "DiscoveryRankings");

            migrationBuilder.DropTable(
                name: "DiscoverySubjectSnapshots");
        }
    }
}

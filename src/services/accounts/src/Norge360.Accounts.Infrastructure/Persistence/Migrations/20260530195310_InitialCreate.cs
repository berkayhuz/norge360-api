// <copyright file="20260530195310_InitialCreate.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Norge360.Accounts.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EventVersion = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RoutingKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    TraceId = table.Column<string>(type: "text", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    LockId = table.Column<Guid>(type: "uuid", nullable: true),
                    LockedUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReservedUsernames",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NormalizedValue = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservedUsernames", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    NormalizedUsername = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Bio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    AvatarStorageKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CoverPhotoUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CoverPhotoStorageKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    District = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Occupation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Company = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Website = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    FollowersCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FollowingCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    PostsCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AccountType = table.Column<short>(type: "smallint", nullable: false),
                    ProfileVisibility = table.Column<short>(type: "smallint", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserFollows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FollowerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FolloweeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFollows", x => x.Id);
                    table.CheckConstraint("CK_UserFollows_FollowerId_NotEqual_FolloweeId", "\"FollowerId\" <> \"FolloweeId\"");
                    table.ForeignKey(
                        name: "FK_UserFollows_UserProfiles_FolloweeId",
                        column: x => x.FolloweeId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserFollows_UserProfiles_FollowerId",
                        column: x => x.FollowerId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UsernameHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldUsername = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    NormalizedOldUsername = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    NewUsername = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    NormalizedNewUsername = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReleasedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsernameHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsernameHistory_UserProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReservedUsernames_NormalizedValue",
                table: "ReservedUsernames",
                column: "NormalizedValue",
                unique: true,
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_UserFollows_FolloweeId",
                table: "UserFollows",
                column: "FolloweeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFollows_FollowerId",
                table: "UserFollows",
                column: "FollowerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFollows_FollowerId_FolloweeId",
                table: "UserFollows",
                columns: new[] { "FollowerId", "FolloweeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserFollows_Status",
                table: "UserFollows",
                column: "Status",
                filter: "\"Status\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_UsernameHistory_NormalizedNewUsername",
                table: "UsernameHistory",
                column: "NormalizedNewUsername");

            migrationBuilder.CreateIndex(
                name: "IX_UsernameHistory_NormalizedOldUsername",
                table: "UsernameHistory",
                column: "NormalizedOldUsername");

            migrationBuilder.CreateIndex(
                name: "IX_UsernameHistory_NormalizedOldUsername_ReleasedAt",
                table: "UsernameHistory",
                columns: new[] { "NormalizedOldUsername", "ReleasedAt" },
                filter: "\"ReleasedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UsernameHistory_ProfileId",
                table: "UsernameHistory",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_AuthUserId",
                table: "UserProfiles",
                column: "AuthUserId",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_IsVerified",
                table: "UserProfiles",
                column: "IsVerified",
                filter: "\"IsVerified\" = true AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_NormalizedUsername",
                table: "UserProfiles",
                column: "NormalizedUsername",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "ReservedUsernames");

            migrationBuilder.DropTable(
                name: "UserFollows");

            migrationBuilder.DropTable(
                name: "UsernameHistory");

            migrationBuilder.DropTable(
                name: "UserProfiles");
        }
    }
}

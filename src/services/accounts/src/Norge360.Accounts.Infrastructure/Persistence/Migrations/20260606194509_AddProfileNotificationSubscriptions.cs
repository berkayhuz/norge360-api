// <copyright file="20260606194509_AddProfileNotificationSubscriptions.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Norge360.Accounts.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileNotificationSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserProfileNotificationSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriberProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfileNotificationSubscriptions", x => x.Id);
                    table.CheckConstraint("CK_UserProfileNotificationSubscriptions_Subscriber_NotEqual_Ta~", "\"SubscriberProfileId\" <> \"TargetProfileId\"");
                    table.ForeignKey(
                        name: "FK_UserProfileNotificationSubscriptions_UserProfiles_Subscribe~",
                        column: x => x.SubscriberProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserProfileNotificationSubscriptions_UserProfiles_TargetPro~",
                        column: x => x.TargetProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfileNotificationSubscriptions_SubscriberProfileId_Ta~",
                table: "UserProfileNotificationSubscriptions",
                columns: new[] { "SubscriberProfileId", "TargetProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfileNotificationSubscriptions_TargetProfileId",
                table: "UserProfileNotificationSubscriptions",
                column: "TargetProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserProfileNotificationSubscriptions");
        }
    }
}

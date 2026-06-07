// <copyright file="20260606194611_InitialNotificationSchemaWithPreferences.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Norge360.Notification.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialNotificationSchemaWithPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InAppNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<short>(type: "smallint", nullable: false),
                    Type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Subject = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Body = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorUsername = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ActorDisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ActorAvatarUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    EntityId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    MetadataJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InAppNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    TextBody = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    HtmlBody = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: true),
                    TemplateKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ChannelsJson = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    RecipientEmailAddress = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    RecipientPhoneNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RecipientPushToken = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    RecipientDisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    MetadataJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    QueuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessingStartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeadLetteredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserNotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    InAppEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PushEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotificationPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationDeliveryAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Provider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Recipient = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ExternalMessageId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDeliveryAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationDeliveryAttempts_Notifications_NotificationMess~",
                        column: x => x.NotificationMessageId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InAppNotifications_UserId_IsRead_CreatedAtUtc",
                table: "InAppNotifications",
                columns: new[] { "UserId", "IsRead", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveryAttempts_NotificationMessageId_Channel",
                table: "NotificationDeliveryAttempts",
                columns: new[] { "NotificationMessageId", "Channel" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IdempotencyKey",
                table: "Notifications",
                column: "IdempotencyKey",
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationPreferences_UserId_Type",
                table: "UserNotificationPreferences",
                columns: new[] { "UserId", "Type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InAppNotifications");

            migrationBuilder.DropTable(
                name: "NotificationDeliveryAttempts");

            migrationBuilder.DropTable(
                name: "UserNotificationPreferences");

            migrationBuilder.DropTable(
                name: "Notifications");
        }
    }
}

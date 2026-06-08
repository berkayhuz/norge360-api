// <copyright file="20260607193000_AddConversationNotificationSoundPreference.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Norge360.MessagingService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationNotificationSoundPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotificationSoundEnabled",
                table: "MessagingConversationParticipants",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationSoundEnabled",
                table: "MessagingConversationParticipants");
        }
    }
}

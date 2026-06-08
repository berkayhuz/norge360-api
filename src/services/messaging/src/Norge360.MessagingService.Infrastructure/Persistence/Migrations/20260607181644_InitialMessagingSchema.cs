// <copyright file="20260607181644_InitialMessagingSchema.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Norge360.MessagingService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialMessagingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MessagingConversationReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportedUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReasonCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    UserProvidedEvidenceCipherText = table.Column<byte[]>(type: "bytea", nullable: true),
                    UserProvidedEvidenceNonce = table.Column<byte[]>(type: "bytea", nullable: true),
                    EvidenceKeyId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessagingConversationReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessagingConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestStatus = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastMessageSenderUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastMessageAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DefaultDisappearingTtlSeconds = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessagingConversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessagingUserDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PublicIdentityKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    SignedPreKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    SignedPreKeySignature = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    OneTimePreKeysJson = table.Column<string>(type: "text", nullable: true),
                    SupportedAlgorithms = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessagingUserDevices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessagingUserSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessagePermission = table.Column<int>(type: "integer", nullable: false),
                    GroupInvitePermission = table.Column<int>(type: "integer", nullable: false),
                    OnlineVisibility = table.Column<int>(type: "integer", nullable: false),
                    ReadReceiptsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    TypingIndicatorsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LinkPreviewsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ShowMessagePreviewInNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessagingUserSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessagingConversationParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    JoinedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LeftAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ArchivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MutedUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PinnedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MarkedUnreadAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClearedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NotificationSoundEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LastReadMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastReadAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastDeliveredMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastDeliveredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NicknameCipherText = table.Column<byte[]>(type: "bytea", nullable: true),
                    NicknameNonce = table.Column<byte[]>(type: "bytea", nullable: true),
                    NicknameKeyId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ThemeKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BackgroundKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessagingConversationParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessagingConversationParticipants_MessagingConversations_Co~",
                        column: x => x.ConversationId,
                        principalTable: "MessagingConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessagingMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderDeviceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ClientMessageId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    CipherText = table.Column<byte[]>(type: "bytea", nullable: false),
                    CipherNonce = table.Column<byte[]>(type: "bytea", nullable: false),
                    CipherKeyId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EncryptionAlgorithm = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AssociatedDataJson = table.Column<string>(type: "text", nullable: true),
                    ClientSearchTokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ReplyToMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    ForwardedFromConversationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ForwardedFromMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    SharedPostId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsForwarded = table.Column<bool>(type: "boolean", nullable: false),
                    IsEdited = table.Column<bool>(type: "boolean", nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    ViewOnce = table.Column<bool>(type: "boolean", nullable: false),
                    DisappearingTtlSeconds = table.Column<int>(type: "integer", nullable: true),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FirstViewedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EditUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RecallUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SentAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EditedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RecalledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AttachmentCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessagingMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessagingMessages_MessagingConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "MessagingConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessagingMessageAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    WaveformJson = table.Column<string>(type: "text", nullable: true),
                    EncryptedFileKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    KeyNonce = table.Column<byte[]>(type: "bytea", nullable: false),
                    KeyId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ViewOnce = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessagingMessageAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessagingMessageAttachments_MessagingMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "MessagingMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessagingMessageReactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Emoji = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessagingMessageReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessagingMessageReactions_MessagingMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "MessagingMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessagingMessageReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReadAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessagingMessageReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessagingMessageReceipts_MessagingMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "MessagingMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MessagingConversationParticipants_ConversationId_UserId",
                table: "MessagingConversationParticipants",
                columns: new[] { "ConversationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessagingConversationParticipants_UserId_DeletedAtUtc_LeftA~",
                table: "MessagingConversationParticipants",
                columns: new[] { "UserId", "DeletedAtUtc", "LeftAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MessagingConversationParticipants_UserId_PinnedAtUtc_Archiv~",
                table: "MessagingConversationParticipants",
                columns: new[] { "UserId", "PinnedAtUtc", "ArchivedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MessagingConversationReports_ConversationId_CreatedAtUtc",
                table: "MessagingConversationReports",
                columns: new[] { "ConversationId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MessagingConversationReports_ReporterUserId_CreatedAtUtc",
                table: "MessagingConversationReports",
                columns: new[] { "ReporterUserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MessagingConversationReports_Status",
                table: "MessagingConversationReports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MessagingConversations_CreatedByUserId",
                table: "MessagingConversations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MessagingConversations_Status_LastMessageAtUtc",
                table: "MessagingConversations",
                columns: new[] { "Status", "LastMessageAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MessagingMessageAttachments_Kind_CreatedAtUtc",
                table: "MessagingMessageAttachments",
                columns: new[] { "Kind", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MessagingMessageAttachments_MessageId_Kind",
                table: "MessagingMessageAttachments",
                columns: new[] { "MessageId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_MessagingMessageReactions_MessageId_UserId_Emoji",
                table: "MessagingMessageReactions",
                columns: new[] { "MessageId", "UserId", "Emoji" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessagingMessageReceipts_MessageId_UserId",
                table: "MessagingMessageReceipts",
                columns: new[] { "MessageId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessagingMessageReceipts_UserId_ReadAtUtc",
                table: "MessagingMessageReceipts",
                columns: new[] { "UserId", "ReadAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MessagingMessages_ConversationId_ClientSearchTokenHash",
                table: "MessagingMessages",
                columns: new[] { "ConversationId", "ClientSearchTokenHash" });

            migrationBuilder.CreateIndex(
                name: "IX_MessagingMessages_ConversationId_CreatedAtUtc",
                table: "MessagingMessages",
                columns: new[] { "ConversationId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MessagingMessages_ConversationId_SenderUserId_ClientMessage~",
                table: "MessagingMessages",
                columns: new[] { "ConversationId", "SenderUserId", "ClientMessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessagingMessages_ExpiresAtUtc",
                table: "MessagingMessages",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_MessagingMessages_ReplyToMessageId",
                table: "MessagingMessages",
                column: "ReplyToMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_MessagingMessages_SharedPostId",
                table: "MessagingMessages",
                column: "SharedPostId");

            migrationBuilder.CreateIndex(
                name: "IX_MessagingUserDevices_UserId_DeviceId",
                table: "MessagingUserDevices",
                columns: new[] { "UserId", "DeviceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessagingUserDevices_UserId_RevokedAtUtc",
                table: "MessagingUserDevices",
                columns: new[] { "UserId", "RevokedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MessagingUserSettings_UserId",
                table: "MessagingUserSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MessagingConversationParticipants");

            migrationBuilder.DropTable(
                name: "MessagingConversationReports");

            migrationBuilder.DropTable(
                name: "MessagingMessageAttachments");

            migrationBuilder.DropTable(
                name: "MessagingMessageReactions");

            migrationBuilder.DropTable(
                name: "MessagingMessageReceipts");

            migrationBuilder.DropTable(
                name: "MessagingUserDevices");

            migrationBuilder.DropTable(
                name: "MessagingUserSettings");

            migrationBuilder.DropTable(
                name: "MessagingMessages");

            migrationBuilder.DropTable(
                name: "MessagingConversations");
        }
    }
}

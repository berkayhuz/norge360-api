// <copyright file="NotificationSchemaCompatibility.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Norge360.Notification.Infrastructure.Persistence;

public static class NotificationSchemaCompatibility
{
    private const string AppliedMigrationId = "20260606194611_InitialNotificationSchemaWithPreferences";
    private const string AppliedProductVersion = "10.0.7";

    public static async Task EnsureCompatibleAsync(
        NotificationDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var sql = """
            ALTER TABLE "InAppNotifications"
              ADD COLUMN IF NOT EXISTS "Category" smallint NOT NULL DEFAULT 5,
              ADD COLUMN IF NOT EXISTS "Type" character varying(128) NOT NULL DEFAULT 'system.legacy',
              ADD COLUMN IF NOT EXISTS "Url" character varying(1024),
              ADD COLUMN IF NOT EXISTS "ActorUserId" uuid,
              ADD COLUMN IF NOT EXISTS "ActorUsername" character varying(128),
              ADD COLUMN IF NOT EXISTS "ActorDisplayName" character varying(256),
              ADD COLUMN IF NOT EXISTS "ActorAvatarUrl" character varying(1024),
              ADD COLUMN IF NOT EXISTS "EntityType" character varying(128),
              ADD COLUMN IF NOT EXISTS "EntityId" character varying(128),
              ADD COLUMN IF NOT EXISTS "MetadataJson" character varying(8000) NOT NULL DEFAULT '{}';

            CREATE TABLE IF NOT EXISTS "Notifications" (
              "Id" uuid NOT NULL,
              "UserId" uuid NULL,
              "Category" integer NOT NULL,
              "Priority" integer NOT NULL,
              "Status" integer NOT NULL,
              "Subject" character varying(512) NOT NULL,
              "TextBody" character varying(8000) NOT NULL,
              "HtmlBody" character varying(16000) NULL,
              "TemplateKey" character varying(256) NULL,
              "ChannelsJson" character varying(512) NOT NULL,
              "RecipientEmailAddress" character varying(320) NULL,
              "RecipientPhoneNumber" character varying(64) NULL,
              "RecipientPushToken" character varying(1024) NULL,
              "RecipientDisplayName" character varying(256) NULL,
              "MetadataJson" character varying(8000) NOT NULL,
              "CorrelationId" character varying(128) NULL,
              "IdempotencyKey" character varying(256) NULL,
              "CreatedAtUtc" timestamp with time zone NOT NULL,
              "CompletedAtUtc" timestamp with time zone NULL,
              "QueuedAtUtc" timestamp with time zone NULL,
              "ProcessingStartedAtUtc" timestamp with time zone NULL,
              "DeadLetteredAtUtc" timestamp with time zone NULL,
              CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "UserNotificationPreferences" (
              "Id" uuid NOT NULL,
              "UserId" uuid NOT NULL,
              "Type" character varying(128) NOT NULL,
              "InAppEnabled" boolean NOT NULL,
              "EmailEnabled" boolean NOT NULL,
              "PushEnabled" boolean NOT NULL,
              "UpdatedAtUtc" timestamp with time zone NOT NULL,
              CONSTRAINT "PK_UserNotificationPreferences" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "NotificationDeliveryAttempts" (
              "Id" uuid NOT NULL,
              "NotificationMessageId" uuid NOT NULL,
              "Channel" integer NOT NULL,
              "Status" integer NOT NULL,
              "Provider" character varying(128) NOT NULL,
              "Recipient" character varying(512) NULL,
              "ExternalMessageId" character varying(256) NULL,
              "ErrorCode" character varying(128) NULL,
              "ErrorMessage" character varying(2000) NULL,
              "AttemptCount" integer NOT NULL,
              "CreatedAtUtc" timestamp with time zone NOT NULL,
              CONSTRAINT "PK_NotificationDeliveryAttempts" PRIMARY KEY ("Id"),
              CONSTRAINT "FK_NotificationDeliveryAttempts_Notifications_NotificationMessa" FOREIGN KEY ("NotificationMessageId") REFERENCES "Notifications" ("Id") ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS "IX_InAppNotifications_UserId_IsRead_CreatedAtUtc" ON "InAppNotifications" ("UserId", "IsRead", "CreatedAtUtc");
            CREATE INDEX IF NOT EXISTS "IX_NotificationDeliveryAttempts_NotificationMessageId_Channel" ON "NotificationDeliveryAttempts" ("NotificationMessageId", "Channel");
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Notifications_IdempotencyKey" ON "Notifications" ("IdempotencyKey") WHERE "IdempotencyKey" IS NOT NULL;
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserNotificationPreferences_UserId_Type" ON "UserNotificationPreferences" ("UserId", "Type");

            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
              "MigrationId" character varying(150) NOT NULL,
              "ProductVersion" character varying(32) NOT NULL,
              CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
            );

            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ('20260606194611_InitialNotificationSchemaWithPreferences', '10.0.7')
            ON CONFLICT ("MigrationId") DO NOTHING;
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        logger.LogInformation("Notification schema compatibility patch applied.");
    }
}

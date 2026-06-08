// <copyright file="MessagingUserSettings.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.MessagingService.Domain.Enums;

namespace Norge360.MessagingService.Domain.Entities;

public sealed class MessagingUserSettings
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public MessagingPermission MessagePermission { get; set; } = MessagingPermission.Everyone;
    public MessagingGroupInvitePermission GroupInvitePermission { get; set; } = MessagingGroupInvitePermission.Everyone;
    public MessagingOnlineVisibility OnlineVisibility { get; set; } = MessagingOnlineVisibility.Mutuals;
    public bool ReadReceiptsEnabled { get; set; } = true;
    public bool TypingIndicatorsEnabled { get; set; } = true;
    public bool LinkPreviewsEnabled { get; set; } = true;
    public bool ShowMessagePreviewInNotifications { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

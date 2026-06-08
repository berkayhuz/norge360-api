// <copyright file="ConversationRequestStatus.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.MessagingService.Domain.Enums;

public enum ConversationRequestStatus
{
    None = 0,
    Pending = 1,
    Accepted = 2,
    Deleted = 3,
    Blocked = 4
}

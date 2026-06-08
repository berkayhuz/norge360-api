// <copyright file="MessageDeliveryState.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.MessagingService.Domain.Enums;

public enum MessageDeliveryState
{
    Queued = 1,
    Sent = 2,
    Delivered = 3,
    Read = 4,
    Failed = 5,
    Recalled = 6
}

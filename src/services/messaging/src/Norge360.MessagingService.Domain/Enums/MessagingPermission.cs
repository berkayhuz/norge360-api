// <copyright file="MessagingPermission.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.MessagingService.Domain.Enums;

public enum MessagingPermission
{
    Everyone = 1,
    Followers = 2,
    Following = 3,
    Mutuals = 4,
    Nobody = 5
}

// <copyright file="MessageKind.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.MessagingService.Domain.Enums;

public enum MessageKind
{
    Text = 1,
    Emoji = 2,
    Media = 3,
    Voice = 4,
    File = 5,
    PostShare = 6,
    ProfileShare = 7,
    Location = 8,
    System = 9
}

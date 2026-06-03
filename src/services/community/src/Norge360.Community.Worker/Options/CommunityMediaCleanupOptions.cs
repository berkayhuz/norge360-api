// <copyright file="CommunityMediaCleanupOptions.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Community.Worker.Options;

public sealed class CommunityMediaCleanupOptions
{
    public const string SectionName = "Community:MediaCleanup";
    public bool Enabled { get; set; } = false;
    public int IntervalMinutes { get; set; } = 60;
    public int SoftDeletedOlderThanHours { get; set; } = 24;
}

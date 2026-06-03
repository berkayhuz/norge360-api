namespace Norge360.Community.Worker.Options;

public sealed class CommunityMediaCleanupOptions
{
    public const string SectionName = "Community:MediaCleanup";
    public bool Enabled { get; set; } = false;
    public int IntervalMinutes { get; set; } = 60;
    public int SoftDeletedOlderThanHours { get; set; } = 24;
}

namespace Norge360.Discovery.Worker.Options;

public sealed class DiscoveryRankingWorkerOptions
{
    public const string SectionName = "Discovery:RankingWorker";
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 300;
}

namespace Norge360.Discovery.API.Security;

public sealed class DiscoveryInternalEventOptions
{
    public const string SectionName = "Security:InternalEvents";

    public bool Enabled { get; set; } = true;

    public string HeaderName { get; set; } = "X-Discovery-Internal-Token";

    public string? Token { get; set; }
}

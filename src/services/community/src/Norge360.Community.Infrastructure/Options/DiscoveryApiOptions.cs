namespace Norge360.Community.Infrastructure.Options;

public sealed class DiscoveryApiOptions
{
    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = "http://localhost:5030";

    public string InternalTokenHeaderName { get; set; } = "X-Discovery-Internal-Token";

    public string? InternalToken { get; set; }
}

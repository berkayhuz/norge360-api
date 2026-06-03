namespace Norge360.Discovery.API.Options;

public sealed class DiscoveryAccountsOptions
{
    public const string SectionName = "Services:Accounts";

    public string? BaseUrl { get; set; }
    public string? InternalTokenHeaderName { get; set; }
    public string? InternalToken { get; set; }
}

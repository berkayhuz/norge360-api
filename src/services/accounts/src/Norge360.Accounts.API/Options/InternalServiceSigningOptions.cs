namespace Norge360.Accounts.API.Options;

public sealed class InternalServiceSigningOptions
{
    public bool Enabled { get; set; }
    public string Secret { get; set; } = string.Empty;
    public int ClockSkewSeconds { get; set; } = 120;
    public string ServiceName { get; set; } = "community-api";
}

namespace Norge360.Auth.API.Security.Turnstile;

public sealed class TurnstileOptions
{
    public const string SectionName = "Cloudflare:Turnstile";

    public bool Enabled { get; set; } = true;

    public string[] AllowedHostnames { get; set; } = [];

    public string SecretKey { get; set; } = string.Empty;
}

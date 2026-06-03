using Norge360.Auth.API.Accessors;
using Norge360.Auth.API.Cookies;
using Norge360.Auth.API.Security.Turnstile;
using Norge360.Auth.Application.DependencyInjection;
using Norge360.Auth.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TurnstileValidationFilter>();
builder.Services.AddControllers(options =>
{
    options.Filters.AddService<TurnstileValidationFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<AuthCookieService>();
builder.Services.AddScoped<AuthRequestContextAccessor>();
builder.Services.AddScoped<ITurnstileVerifier, CloudflareTurnstileVerifier>();
builder.Services.AddHttpClient(nameof(CloudflareTurnstileVerifier), client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddOptions<TurnstileOptions>()
    .Bind(builder.Configuration.GetSection(TurnstileOptions.SectionName))
    .ValidateOnStart();
builder.Services.PostConfigure<TurnstileOptions>(options =>
{
    var envSecret = Environment.GetEnvironmentVariable("CLOUDFLARE_TURNSTILE_SECRET_KEY");
    if (!string.IsNullOrWhiteSpace(envSecret))
    {
        options.SecretKey = envSecret;
    }
});
builder.Services.AddSingleton<IValidateOptions<TurnstileOptions>, TurnstileOptionsValidation>();
builder.Services.AddAuthApplication();
builder.Services.AddAuthInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
await app.RunAsync();

public partial class Program;

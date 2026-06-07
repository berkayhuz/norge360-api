// <copyright file="InfrastructureDependencyInjection.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.StackExchangeRedis;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Norge360.Auth.Application.Abstractions;
using Norge360.Auth.Application.Options;
using Norge360.Auth.Domain.Entities;
using Norge360.Auth.Infrastructure.Persistence;
using Norge360.Auth.Infrastructure.Services;
using Norge360.Clock;
using Norge360.Messaging.RabbitMq.DependencyInjection;
using StackExchange.Redis;

namespace Norge360.Auth.Infrastructure.DependencyInjection;

public static class InfrastructureDependencyInjection
{
    private const string AccountsUsernameResolverClientName = "accounts-username-resolver";

    public static IServiceCollection AddAuthInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName)).ValidateOnStart();
        services.AddOptions<TokenTransportOptions>().Bind(configuration.GetSection(TokenTransportOptions.SectionName)).ValidateOnStart();
        services.AddOptions<PasswordPolicyOptions>().Bind(configuration.GetSection(PasswordPolicyOptions.SectionName)).ValidateOnStart();
        services.AddOptions<OutboxOptions>().Bind(configuration.GetSection(OutboxOptions.SectionName)).ValidateOnStart();
        services.AddOptions<DatabaseOptions>().Bind(configuration.GetSection(DatabaseOptions.SectionName)).ValidateOnStart();
        services.AddOptions<DistributedCacheOptions>().Bind(configuration.GetSection(DistributedCacheOptions.SectionName)).ValidateOnStart();
        services.AddOptions<AuthDataProtectionOptions>().Bind(configuration.GetSection(AuthDataProtectionOptions.SectionName)).ValidateOnStart();
        services.AddSingleton<IValidateOptions<DatabaseOptions>, DatabaseOptionsValidation>();

        var connectionString = configuration.GetConnectionString("IdentityConnection")
            ?? throw new InvalidOperationException("Connection string 'IdentityConnection' is missing.");
        var distributedCacheOptions = configuration.GetSection(DistributedCacheOptions.SectionName).Get<DistributedCacheOptions>()
            ?? throw new InvalidOperationException("Distributed cache options are missing.");
        var authDataProtectionOptions = configuration.GetSection(AuthDataProtectionOptions.SectionName).Get<AuthDataProtectionOptions>()
            ?? throw new InvalidOperationException("Auth data protection options are missing.");

        if (string.Equals(distributedCacheOptions.Provider, "Redis", StringComparison.OrdinalIgnoreCase))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = distributedCacheOptions.RedisConnectionString;
                options.InstanceName = distributedCacheOptions.InstanceName;
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IAuthUnitOfWork>(sp => sp.GetRequiredService<AuthDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUsernameLoginResolver, AccountsUsernameLoginResolver>();
        services.AddScoped<IAuthUserProfileResolver, AccountsAuthUserProfileResolver>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<IUserMfaRecoveryCodeRepository, UserMfaRecoveryCodeRepository>();
        services.AddScoped<ITrustedDeviceRepository, TrustedDeviceRepository>();
        services.AddScoped<IAccessTokenFactory, JwtAccessTokenFactory>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IAuthenticatorTotpService, AuthenticatorTotpService>();
        services.AddScoped<IAuthenticatorKeyProtector, AuthenticatorKeyProtector>();
        services.AddScoped<IRecoveryCodeService, RecoveryCodeService>();
        services.AddScoped<ISecurityAlertPublisher, SecurityAlertPublisher>();
        services.AddScoped<OutboxPayloadProtector>();
        services.AddScoped<IIntegrationEventOutbox, IntegrationEventOutbox>();
        services.AddScoped<OutboxMessagePublisher>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ITokenSigningKeyProvider, TokenSigningKeyProvider>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        var redisConnectionString = authDataProtectionOptions.RedisConnectionString;
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            redisConnectionString = distributedCacheOptions.RedisConnectionString;
        }

        IConnectionMultiplexer? redisConnectionMultiplexer = null;
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            redisConnectionMultiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
            services.AddSingleton(redisConnectionMultiplexer);
        }

        var dataProtectionBuilder = services.AddDataProtection()
            .SetApplicationName(authDataProtectionOptions.ApplicationName);

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            dataProtectionBuilder.PersistKeysToStackExchangeRedis(
                redisConnectionMultiplexer ?? throw new InvalidOperationException("Redis connection multiplexer is missing."),
                $"{authDataProtectionOptions.ApplicationName}:DataProtection:KeyRing");
        }
        else if (!string.IsNullOrWhiteSpace(authDataProtectionOptions.KeyRingPath))
        {
            dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(authDataProtectionOptions.KeyRingPath));
        }

        services.AddHttpClient(AccountsUsernameResolverClientName, client =>
        {
            var baseUrl = configuration["Auth:Login:AccountsGatewayBaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = "http://localhost:5030/";
            }

            client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddRabbitMqMessaging(configuration);
        services.AddHostedService<OutboxPublisherService>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>, IOptions<TokenTransportOptions>, ITokenSigningKeyProvider>(
                (options, jwtOptionsAccessor, tokenTransportAccessor, tokenSigningKeyProvider) =>
                {
                    var jwtOptions = jwtOptionsAccessor.Value;
                    var tokenTransport = tokenTransportAccessor.Value;

                    options.RequireHttpsMetadata = false;
                    options.SaveToken = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKeys = tokenSigningKeyProvider.GetValidationKeys(),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30),
                        NameClaimType = ClaimTypes.NameIdentifier,
                        RoleClaimType = ClaimTypes.Role
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            if (string.IsNullOrWhiteSpace(context.Token) &&
                                context.Request.Cookies.TryGetValue(tokenTransport.AccessCookieName, out var cookieToken))
                            {
                                context.Token = cookieToken;
                            }

                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            var subjectClaim = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                                               context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                            var sessionClaim = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sid)?.Value;
                            var tokenVersionClaim = context.Principal?.FindFirst("token_version")?.Value;

                            if (!Guid.TryParse(subjectClaim, out _) ||
                                !Guid.TryParse(sessionClaim, out _) ||
                                !int.TryParse(tokenVersionClaim, out _))
                            {
                                context.Fail("JWT principal claims are incomplete.");
                            }

                            return Task.CompletedTask;
                        }
                    };
                });

        services.AddAuthorization();
        return services;
    }
}

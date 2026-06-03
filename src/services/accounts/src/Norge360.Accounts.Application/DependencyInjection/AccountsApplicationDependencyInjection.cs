// <copyright file="AccountsApplicationDependencyInjection.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Application.Services;

namespace Norge360.Accounts.Application.DependencyInjection;

public static class AccountsApplicationDependencyInjection
{
    public static IServiceCollection AddAccountsApplication(this IServiceCollection services)
    {
        services.AddScoped<IUsernameNormalizer, UsernameNormalizer>();
        services.AddScoped<IUsernameValidator, UsernameValidator>();
        services.AddScoped<IUsernameAvailabilityService, UsernameAvailabilityService>();
        services.AddScoped<IProfileProvisioningService, ProfileProvisioningService>();
        services.AddScoped<IProfileVisibilityPolicy, ProfileVisibilityPolicy>();
        services.AddScoped<IProfileQueryService, ProfileQueryService>();
        services.AddScoped<IProfileViewService, ProfileViewService>();
        services.AddScoped<IProfileAvatarUploadIntentService, ProfileAvatarUploadIntentService>();
        services.AddScoped<IUpdateMyProfileRequestValidator, UpdateMyProfileRequestValidator>();
        services.AddScoped<IProfileMutationService, ProfileMutationService>();
        services.AddScoped<IUserBlockService, UserBlockService>();
        services.AddScoped<IUserFollowService, UserFollowService>();
        services.AddScoped<IUserSearchReindexService, UserSearchReindexService>();
        return services;
    }
}

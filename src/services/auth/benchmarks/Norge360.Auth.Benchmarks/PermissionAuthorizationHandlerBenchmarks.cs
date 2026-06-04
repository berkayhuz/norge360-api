// <copyright file="PermissionAuthorizationHandlerBenchmarks.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.AspNetCore.Authorization;
using Norge360.Auth.API.Permissions;

namespace Norge360.Auth.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class PermissionAuthorizationHandlerBenchmarks
{
    private readonly PermissionAuthorizationHandler _handler = new();
    private PermissionRequirement _matchingRequirement = null!;
    private PermissionRequirement _missingRequirement = null!;
    private ClaimsPrincipal _matchingUser = null!;
    private ClaimsPrincipal _wildcardUser = null!;
    private ClaimsPrincipal _missingUser = null!;

    [GlobalSetup]
    public void Setup()
    {
        _matchingRequirement = new PermissionRequirement("users.read");
        _missingRequirement = new PermissionRequirement("users.delete");
        _matchingUser = CreateUser("users.read");
        _wildcardUser = CreateUser("*");
        _missingUser = CreateUser("users.write");
    }

    [Benchmark]
    public async Task Allow_WithMatchingPermission()
    {
        var context = new AuthorizationHandlerContext([_matchingRequirement], _matchingUser, null);
        await _handler.HandleAsync(context);
    }

    [Benchmark]
    public async Task Allow_WithWildcardPermission()
    {
        var context = new AuthorizationHandlerContext([_matchingRequirement], _wildcardUser, null);
        await _handler.HandleAsync(context);
    }

    [Benchmark]
    public async Task Deny_WithoutMatchingPermission()
    {
        var context = new AuthorizationHandlerContext([_missingRequirement], _missingUser, null);
        await _handler.HandleAsync(context);
    }

    private static ClaimsPrincipal CreateUser(string permission)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("permission", permission)
        ], "bench"));
    }
}

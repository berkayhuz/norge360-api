// <copyright file="AuthorizationCatalogTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using FluentAssertions;
using Microsoft.Extensions.Options;
using Norge360.Auth.Application.Features.Commands;
using Norge360.Auth.Application.Options;
using Norge360.Auth.Application.Security;
using Norge360.Auth.Application.Validators;

namespace Norge360.Auth.Application.UnitTests;

public sealed class AuthorizationCatalogTests
{
    [Fact]
    public void FindRole_Should_Be_CaseInsensitive()
    {
        var role = AuthorizationCatalog.FindRole("PLATFORM-ADMIN");

        role.Should().NotBeNull();
        role!.Name.Should().Be(AuthorizationCatalog.Roles.PlatformAdmin);
        role.Rank.Should().Be(80);
    }

    [Fact]
    public void PermissionCatalog_Should_Contain_Wildcard_Only_Once()
    {
        var permissions = AuthorizationCatalog.PermissionCatalog;

        permissions.Should().Contain(AuthorizationCatalog.WildcardPermission);
        permissions.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void ResolvePermissions_Should_Combine_Role_And_Explicit_Permissions()
    {
        var permissions = AuthorizationCatalog.ResolvePermissions(
            [AuthorizationCatalog.Roles.PlatformUser, AuthorizationCatalog.Roles.PlatformAdmin],
            ["custom.permission", "  custom.permission  ", null!]);

        permissions.Should().Contain(AuthorizationCatalog.Permissions.ProfileSelf);
        permissions.Should().Contain(AuthorizationCatalog.Permissions.UsersManage);
        permissions.Should().Contain("custom.permission");
        permissions.Should().OnlyHaveUniqueItems();
    }
}

public sealed class AuthValidatorTests
{
    [Fact]
    public void LoginCommandValidator_Should_Reject_Empty_Fields()
    {
        var validator = new LoginCommandValidator();

        var result = validator.Validate(new LoginCommand(string.Empty, string.Empty, false, null, null, null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == nameof(LoginCommand.EmailOrUserName));
        result.Errors.Should().ContainSingle(x => x.PropertyName == nameof(LoginCommand.Password));
    }

    [Fact]
    public void RegisterCommandValidator_Should_Reject_Unsupported_Culture()
    {
        var validator = new RegisterCommandValidator(Microsoft.Extensions.Options.Options.Create(new PasswordPolicyOptions()));

        var result = validator.Validate(new RegisterCommand(
            "berkay",
            "berkay@norge360.com",
            "StrongPassword123!",
            null,
            null,
            "xx-XX",
            null,
            null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == nameof(RegisterCommand.Culture));
    }

    [Fact]
    public void RegisterCommandValidator_Should_Accept_Minimal_Valid_Input()
    {
        var validator = new RegisterCommandValidator(Microsoft.Extensions.Options.Options.Create(new PasswordPolicyOptions
        {
            MinimumLength = 8
        }));

        var result = validator.Validate(new RegisterCommand(
            "berkay",
            "berkay@norge360.com",
            "StrongPassword123!",
            "  Berkay   Huzer  ",
            null,
            null,
            null,
            null));

        result.IsValid.Should().BeTrue();
    }
}

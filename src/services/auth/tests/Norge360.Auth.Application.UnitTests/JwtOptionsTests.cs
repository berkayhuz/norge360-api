// <copyright file="JwtOptionsTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using FluentAssertions;
using Norge360.Auth.Application.Options;

namespace Norge360.Auth.Application.UnitTests;

public sealed class JwtOptionsTests
{
    [Fact]
    public void Defaults_Should_Use_Longer_Normal_And_Persistent_Refresh_Lifetimes()
    {
        var options = new JwtOptions();

        options.AccessTokenMinutes.Should().Be(15);
        options.RefreshTokenHours.Should().Be(12);
        options.RefreshTokenPersistentDays.Should().Be(7);
    }
}

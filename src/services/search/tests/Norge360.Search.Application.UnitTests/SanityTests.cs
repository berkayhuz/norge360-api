// <copyright file="SanityTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Search.Application.DependencyInjection;

namespace Norge360.Search.Application.UnitTests;

public sealed class SanityTests
{
    [Fact]
    public void AddSearchApplication_Should_Return_ServiceCollection()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();

        var result = services.AddSearchApplication();

        Assert.Same(services, result);
    }
}

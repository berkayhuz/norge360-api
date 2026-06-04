// <copyright file="DesignArchitectureTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Idempotency.Redis.Architecture.Tests;

public class DesignArchitectureTests
{
    [Fact]
    public void RedisIdempotencyStateStore_should_be_sealed()
    {
        Assert.True(typeof(RedisIdempotencyStateStore).IsSealed, "RedisIdempotencyStateStore should remain sealed.");
    }
}

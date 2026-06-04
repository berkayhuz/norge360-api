// <copyright file="DesignArchitectureTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Idempotency.Architecture.Tests;

public class DesignArchitectureTests
{
    private sealed record SampleRequest(string Value) : IIdempotentCommand
    {
        public string? IdempotencyKey => "k";
    }

    private sealed record SampleResponse(string Value);

    [Fact]
    public void IdempotencyBehavior_should_be_sealed()
    {
        var type = typeof(IdempotencyBehavior<SampleRequest, SampleResponse>);
        Assert.True(type.IsSealed, "IdempotencyBehavior should remain sealed.");
    }

    [Fact]
    public void IdempotencyState_should_remain_record_and_factories_should_set_status()
    {
        var inProgress = IdempotencyState.InProgress("h");
        var completed = IdempotencyState.Completed("h", "{}");

        Assert.Equal(IdempotencyStatus.InProgress, inProgress.Status);
        Assert.Equal(IdempotencyStatus.Completed, completed.Status);
    }
}

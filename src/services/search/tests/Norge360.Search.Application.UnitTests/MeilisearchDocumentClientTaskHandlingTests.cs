// <copyright file="MeilisearchDocumentClientTaskHandlingTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using FluentAssertions;
using Meilisearch;
using Norge360.Search.Infrastructure.Meilisearch.Client;

namespace Norge360.Search.Application.UnitTests;

public sealed class MeilisearchDocumentClientTaskHandlingTests
{
    [Theory]
    [InlineData(TaskInfoStatus.Succeeded)]
    [InlineData(TaskInfoStatus.Enqueued)]
    [InlineData(TaskInfoStatus.Processing)]
    public void EnsureTaskCompletedSuccessfully_ShouldNotThrow_ForNonFailureStates(TaskInfoStatus status)
    {
        var action = () => MeilisearchDocumentClient.EnsureTaskCompletedSuccessfully(42, status, null);

        action.Should().NotThrow();
    }

    [Theory]
    [InlineData(TaskInfoStatus.Failed)]
    [InlineData(TaskInfoStatus.Canceled)]
    public void EnsureTaskCompletedSuccessfully_ShouldThrow_ForFailedStates(TaskInfoStatus status)
    {
        var action = () => MeilisearchDocumentClient.EnsureTaskCompletedSuccessfully(
            73,
            status,
            new Dictionary<string, string>
            {
                ["code"] = "index_not_found",
                ["message"] = "Index does not exist."
            });

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*73*")
            .WithMessage($"*{status}*")
            .WithMessage("*index_not_found*");
    }
}

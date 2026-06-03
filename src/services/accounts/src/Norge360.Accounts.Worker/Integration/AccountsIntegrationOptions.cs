// <copyright file="AccountsIntegrationOptions.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Accounts.Worker.Integration;

public sealed class AccountsIntegrationOptions
{
    public const string SectionName = "AccountsIntegration";

    public string Exchange { get; init; } = "Norge360.integration";

    public string QueueName { get; init; } = "accounts.user-registered.v1";

    public string RetryQueue1 { get; init; } = "accounts.user-registered.v1.retry.1";

    public string RetryQueue2 { get; init; } = "accounts.user-registered.v1.retry.2";

    public string DeadLetterQueue { get; init; } = "accounts.user-registered.v1.dlq";

    public string RoutingKey { get; init; } = "auth.user.registered.v1";

    public string RetryRoutingKey1 { get; init; } = "accounts.user-registered.v1.retry.1";

    public string RetryRoutingKey2 { get; init; } = "accounts.user-registered.v1.retry.2";

    public string DeadLetterRoutingKey { get; init; } = "accounts.user-registered.v1.dlq";

    public TimeSpan RetryDelay1 { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan RetryDelay2 { get; init; } = TimeSpan.FromMinutes(5);

    public int MaxRetryAttempts { get; init; } = 2;

    public ushort PrefetchCount { get; init; } = 10;

    public string ConsumerTag { get; init; } = "norge360.accounts.user-registered";
}

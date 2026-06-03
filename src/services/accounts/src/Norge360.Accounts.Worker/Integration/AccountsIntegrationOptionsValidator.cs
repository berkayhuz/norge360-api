// <copyright file="AccountsIntegrationOptionsValidator.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.Extensions.Options;

namespace Norge360.Accounts.Worker.Integration;

public sealed class AccountsIntegrationOptionsValidator : IValidateOptions<AccountsIntegrationOptions>
{
    private const string ExpectedUserRegisteredRoutingKey = "auth.user.registered.v1";

    public ValidateOptionsResult Validate(string? name, AccountsIntegrationOptions options)
    {
        var failures = new List<string>();

        ValidateRequired(options.Exchange, "AccountsIntegration:Exchange", failures);
        ValidateRequired(options.QueueName, "AccountsIntegration:QueueName", failures);
        ValidateRequired(options.RetryQueue1, "AccountsIntegration:RetryQueue1", failures);
        ValidateRequired(options.RetryQueue2, "AccountsIntegration:RetryQueue2", failures);
        ValidateRequired(options.DeadLetterQueue, "AccountsIntegration:DeadLetterQueue", failures);
        ValidateRequired(options.RetryRoutingKey1, "AccountsIntegration:RetryRoutingKey1", failures);
        ValidateRequired(options.RetryRoutingKey2, "AccountsIntegration:RetryRoutingKey2", failures);
        ValidateRequired(options.DeadLetterRoutingKey, "AccountsIntegration:DeadLetterRoutingKey", failures);
        ValidateRequired(options.ConsumerTag, "AccountsIntegration:ConsumerTag", failures);

        if (!string.Equals(options.RoutingKey, ExpectedUserRegisteredRoutingKey, StringComparison.Ordinal))
        {
            failures.Add($"AccountsIntegration:RoutingKey must be '{ExpectedUserRegisteredRoutingKey}'.");
        }

        if (options.RetryDelay1 <= TimeSpan.Zero)
        {
            failures.Add("AccountsIntegration:RetryDelay1 must be greater than zero.");
        }

        if (options.RetryDelay2 <= options.RetryDelay1)
        {
            failures.Add("AccountsIntegration:RetryDelay2 must be greater than AccountsIntegration:RetryDelay1.");
        }

        if (options.RetryDelay1.TotalMilliseconds > int.MaxValue ||
            options.RetryDelay2.TotalMilliseconds > int.MaxValue)
        {
            failures.Add("AccountsIntegration retry delays must fit RabbitMQ x-message-ttl millisecond values.");
        }

        if (options.MaxRetryAttempts < 0)
        {
            failures.Add("AccountsIntegration:MaxRetryAttempts must be greater than or equal to zero.");
        }

        if (options.PrefetchCount == 0)
        {
            failures.Add("AccountsIntegration:PrefetchCount must be greater than zero.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateRequired(string value, string settingName, ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"{settingName} is required.");
        }
    }
}

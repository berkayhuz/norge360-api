// <copyright file="UserRegisteredMessageProcessor.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Norge360.Accounts.Application.Abstractions;
using Norge360.Accounts.Contracts.IntegrationEvents.V1;

namespace Norge360.Accounts.Worker.Integration;

public sealed class UserRegisteredMessageProcessor(
    IServiceScopeFactory scopeFactory,
    IOptions<AccountsIntegrationOptions> options,
    ILogger<UserRegisteredMessageProcessor> logger) : IUserRegisteredMessageProcessor
{
    private const string PostgresUniqueViolation = "23505";
    private const string AuthUserIdUniqueConstraintNameFragment = "AuthUserId";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<UserRegisteredProcessingResult> ProcessAsync(
        ReadOnlyMemory<byte> body,
        UserRegisteredMessageMetadata metadata,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var metadataValidation = ValidateMetadata(metadata);
        if (metadataValidation is not null)
        {
            logger.LogWarning(
                "UserRegisteredV1 metadata validation failed. Reason={Reason} RoutingKey={RoutingKey} MessageId={MessageId} CorrelationId={CorrelationId}",
                metadataValidation.Reason,
                metadata.RoutingKey,
                metadata.MessageId,
                metadata.CorrelationId);
            return metadataValidation;
        }

        UserRegisteredV1 message;
        try
        {
            message = JsonSerializer.Deserialize<UserRegisteredV1>(body.Span, SerializerOptions)
                ?? throw new JsonException("UserRegisteredV1 payload deserialized to null.");
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "UserRegisteredV1 payload could not be deserialized. RoutingKey={RoutingKey} MessageId={MessageId} CorrelationId={CorrelationId}",
                metadata.RoutingKey,
                metadata.MessageId,
                metadata.CorrelationId);
            return UserRegisteredProcessingResult.PermanentFailure("invalid_payload", exception: exception);
        }

        var payloadValidation = ValidatePayload(message);
        if (payloadValidation is not null)
        {
            logger.LogWarning(
                "UserRegisteredV1 payload validation failed. Reason={Reason} UserId={UserId} MessageId={MessageId} CorrelationId={CorrelationId}",
                payloadValidation.Reason,
                message.UserId,
                metadata.MessageId,
                metadata.CorrelationId);
            return payloadValidation;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var provisioningService = scope.ServiceProvider.GetRequiredService<IProfileProvisioningService>();
            var profile = await provisioningService.ProvisionAsync(message, cancellationToken);

            logger.LogInformation(
                "UserRegisteredV1 processed. AuthUserId={AuthUserId} ProfileId={ProfileId} MessageId={MessageId} CorrelationId={CorrelationId}",
                message.UserId,
                profile.Id,
                metadata.MessageId,
                metadata.CorrelationId);

            return UserRegisteredProcessingResult.Success(message.UserId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DbUpdateException exception) when (IsAuthUserIdDuplicate(exception))
        {
            logger.LogInformation(
                exception,
                "UserRegisteredV1 treated as idempotent success after duplicate AuthUserId. AuthUserId={AuthUserId} MessageId={MessageId} CorrelationId={CorrelationId}",
                message.UserId,
                metadata.MessageId,
                metadata.CorrelationId);
            return UserRegisteredProcessingResult.Success(message.UserId, "duplicate_auth_user_id");
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            logger.LogWarning(
                exception,
                "UserRegisteredV1 hit a unique constraint race and will be classified as transient. AuthUserId={AuthUserId} MessageId={MessageId} CorrelationId={CorrelationId}",
                message.UserId,
                metadata.MessageId,
                metadata.CorrelationId);
            return UserRegisteredProcessingResult.TransientFailure("unique_constraint_race", message.UserId, exception);
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(
                exception,
                "UserRegisteredV1 database update failed and will be classified as transient. AuthUserId={AuthUserId} MessageId={MessageId} CorrelationId={CorrelationId}",
                message.UserId,
                metadata.MessageId,
                metadata.CorrelationId);
            return UserRegisteredProcessingResult.TransientFailure("db_update_failed", message.UserId, exception);
        }
        catch (TimeoutException exception)
        {
            logger.LogWarning(
                exception,
                "UserRegisteredV1 processing timed out. AuthUserId={AuthUserId} MessageId={MessageId} CorrelationId={CorrelationId}",
                message.UserId,
                metadata.MessageId,
                metadata.CorrelationId);
            return UserRegisteredProcessingResult.TransientFailure("timeout", message.UserId, exception);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "UserRegisteredV1 processing failed unexpectedly and will be classified as transient. AuthUserId={AuthUserId} MessageId={MessageId} CorrelationId={CorrelationId}",
                message.UserId,
                metadata.MessageId,
                metadata.CorrelationId);
            return UserRegisteredProcessingResult.TransientFailure("unexpected_error", message.UserId, exception);
        }
    }

    private UserRegisteredProcessingResult? ValidateMetadata(UserRegisteredMessageMetadata metadata)
    {
        if (!string.Equals(metadata.RoutingKey, options.Value.RoutingKey, StringComparison.Ordinal))
        {
            return UserRegisteredProcessingResult.PermanentFailure("unsupported_routing_key");
        }

        var eventName = metadata.EventName ?? TryGetStringHeader(metadata.Headers, "event_name");
        if (!string.Equals(eventName, UserRegisteredV1.EventName, StringComparison.Ordinal))
        {
            return UserRegisteredProcessingResult.PermanentFailure("unsupported_event_name");
        }

        var eventVersion = metadata.EventVersion ?? TryGetIntHeader(metadata.Headers, "event_version");
        if (eventVersion != UserRegisteredV1.EventVersion)
        {
            return UserRegisteredProcessingResult.PermanentFailure("unsupported_event_version");
        }

        return null;
    }

    private static UserRegisteredProcessingResult? ValidatePayload(UserRegisteredV1 message)
    {
        if (message.UserId == Guid.Empty)
        {
            return UserRegisteredProcessingResult.PermanentFailure("missing_user_id", message.UserId);
        }

        if (string.IsNullOrWhiteSpace(message.Email))
        {
            return UserRegisteredProcessingResult.PermanentFailure("missing_email", message.UserId);
        }

        if (message.RegisteredAtUtc == default)
        {
            return UserRegisteredProcessingResult.PermanentFailure("missing_registered_at", message.UserId);
        }

        return null;
    }

    private static bool IsAuthUserIdDuplicate(DbUpdateException exception) =>
        IsUniqueViolation(exception) &&
        TryGetPostgresConstraintName(exception) is { } constraintName &&
        constraintName.Contains(AuthUserIdUniqueConstraintNameFragment, StringComparison.OrdinalIgnoreCase);

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        string.Equals(TryGetPostgresSqlState(exception), PostgresUniqueViolation, StringComparison.Ordinal);

    private static string? TryGetPostgresSqlState(Exception exception) =>
        TryGetPostgresExceptionProperty(exception, "SqlState");

    private static string? TryGetPostgresConstraintName(Exception exception) =>
        TryGetPostgresExceptionProperty(exception, "ConstraintName");

    private static string? TryGetPostgresExceptionProperty(Exception exception, string propertyName)
    {
        var current = exception;
        while (current is not null)
        {
            if (string.Equals(current.GetType().FullName, "Npgsql.PostgresException", StringComparison.Ordinal))
            {
                return current.GetType().GetProperty(propertyName)?.GetValue(current) as string;
            }

            current = current.InnerException;
        }

        return null;
    }

    private static string? TryGetStringHeader(IReadOnlyDictionary<string, object?> headers, string key)
    {
        if (!headers.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            string stringValue => stringValue,
            byte[] bytes => System.Text.Encoding.UTF8.GetString(bytes),
            ReadOnlyMemory<byte> bytes => System.Text.Encoding.UTF8.GetString(bytes.Span),
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
        };
    }

    private static int? TryGetIntHeader(IReadOnlyDictionary<string, object?> headers, string key)
    {
        var value = TryGetStringHeader(headers, key);
        return int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }
}

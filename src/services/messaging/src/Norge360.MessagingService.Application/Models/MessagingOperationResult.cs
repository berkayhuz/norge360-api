// <copyright file="MessagingOperationResult.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.MessagingService.Application.Models;

public enum MessagingOperationStatus
{
    Success = 1,
    Unauthorized = 2,
    Forbidden = 3,
    NotFound = 4,
    ValidationFailed = 5,
    Conflict = 6,
    Expired = 7
}

public sealed record MessagingOperationResult<T>(
    MessagingOperationStatus Status,
    T? Value,
    string? ErrorCode)
{
    public bool Succeeded => Status == MessagingOperationStatus.Success;

    public static MessagingOperationResult<T> Success(T value) => new(MessagingOperationStatus.Success, value, null);
    public static MessagingOperationResult<T> Unauthorized(string errorCode) => new(MessagingOperationStatus.Unauthorized, default, errorCode);
    public static MessagingOperationResult<T> Forbidden(string errorCode) => new(MessagingOperationStatus.Forbidden, default, errorCode);
    public static MessagingOperationResult<T> NotFound(string errorCode) => new(MessagingOperationStatus.NotFound, default, errorCode);
    public static MessagingOperationResult<T> ValidationFailed(string errorCode) => new(MessagingOperationStatus.ValidationFailed, default, errorCode);
    public static MessagingOperationResult<T> Conflict(string errorCode) => new(MessagingOperationStatus.Conflict, default, errorCode);
    public static MessagingOperationResult<T> Expired(string errorCode) => new(MessagingOperationStatus.Expired, default, errorCode);
}

public sealed record MessagingOperationResult(
    MessagingOperationStatus Status,
    string? ErrorCode)
{
    public bool Succeeded => Status == MessagingOperationStatus.Success;

    public static MessagingOperationResult Success() => new(MessagingOperationStatus.Success, null);
    public static MessagingOperationResult Unauthorized(string errorCode) => new(MessagingOperationStatus.Unauthorized, errorCode);
    public static MessagingOperationResult Forbidden(string errorCode) => new(MessagingOperationStatus.Forbidden, errorCode);
    public static MessagingOperationResult NotFound(string errorCode) => new(MessagingOperationStatus.NotFound, errorCode);
    public static MessagingOperationResult ValidationFailed(string errorCode) => new(MessagingOperationStatus.ValidationFailed, errorCode);
    public static MessagingOperationResult Conflict(string errorCode) => new(MessagingOperationStatus.Conflict, errorCode);
    public static MessagingOperationResult Expired(string errorCode) => new(MessagingOperationStatus.Expired, errorCode);
}

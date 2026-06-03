// <copyright file="UserRegisteredProcessingResult.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Accounts.Worker.Integration;

public enum UserRegisteredProcessingStatus
{
    Success = 0,
    PermanentFailure = 1,
    TransientFailure = 2
}

public sealed record UserRegisteredProcessingResult(
    UserRegisteredProcessingStatus Status,
    string Reason,
    Guid? UserId = null,
    Exception? Exception = null)
{
    public bool ShouldRetry => Status == UserRegisteredProcessingStatus.TransientFailure;

    public static UserRegisteredProcessingResult Success(Guid userId, string reason = "processed") =>
        new(UserRegisteredProcessingStatus.Success, reason, userId);

    public static UserRegisteredProcessingResult PermanentFailure(
        string reason,
        Guid? userId = null,
        Exception? exception = null) =>
        new(UserRegisteredProcessingStatus.PermanentFailure, reason, userId, exception);

    public static UserRegisteredProcessingResult TransientFailure(
        string reason,
        Guid? userId = null,
        Exception? exception = null) =>
        new(UserRegisteredProcessingStatus.TransientFailure, reason, userId, exception);
}

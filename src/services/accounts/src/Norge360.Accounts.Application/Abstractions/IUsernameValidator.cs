// <copyright file="IUsernameValidator.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Accounts.Application.Abstractions;

public interface IUsernameValidator
{
    UsernameValidationResult Validate(string? username);
}

public sealed record UsernameValidationResult(bool IsValid, string? Reason)
{
    public static UsernameValidationResult Valid() => new(true, null);

    public static UsernameValidationResult Invalid(string reason) => new(false, reason);
}

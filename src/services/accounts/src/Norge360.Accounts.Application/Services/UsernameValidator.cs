// <copyright file="UsernameValidator.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Text.RegularExpressions;
using Norge360.Accounts.Application.Abstractions;

namespace Norge360.Accounts.Application.Services;

public sealed partial class UsernameValidator : IUsernameValidator
{
    public const int MinimumLength = 3;
    public const int MaximumLength = 30;

    public UsernameValidationResult Validate(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return UsernameValidationResult.Invalid("username_required");
        }

        var value = username.Trim();
        if (value.Length is < MinimumLength or > MaximumLength)
        {
            return UsernameValidationResult.Invalid("username_length_invalid");
        }

        return UsernameRegex().IsMatch(value)
            ? UsernameValidationResult.Valid()
            : UsernameValidationResult.Invalid("username_format_invalid");
    }

    [GeneratedRegex("^[a-zA-Z0-9][a-zA-Z0-9_-]{1,28}[a-zA-Z0-9]$")]
    private static partial Regex UsernameRegex();
}

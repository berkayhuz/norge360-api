// <copyright file="IUpdateMyProfileRequestValidator.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Accounts.Contracts.Requests;

namespace Norge360.Accounts.Application.Abstractions;

public interface IUpdateMyProfileRequestValidator
{
    UpdateMyProfileRequestValidationResult Validate(UpdateMyProfileRequest request);
}

public sealed record UpdateMyProfileRequestValidationResult(
    bool IsValid,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public static UpdateMyProfileRequestValidationResult Success() =>
        new(true, new Dictionary<string, string[]>());

    public static UpdateMyProfileRequestValidationResult Failure(
        IReadOnlyDictionary<string, string[]> errors) =>
        new(false, errors);
}

// <copyright file="RequireTurnstileAttribute.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Norge360.Auth.API.Security.Turnstile;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RequireTurnstileAttribute : Attribute, IFilterMetadata
{
}

using System;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Norge360.Auth.API.Security.Turnstile;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RequireTurnstileAttribute : Attribute, IFilterMetadata
{
}

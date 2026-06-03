using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Norge360.Auth.Contracts.Requests;

namespace Norge360.Auth.API.Security.Turnstile;

public sealed class TurnstileValidationFilter(ITurnstileVerifier turnstileVerifier) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!RequiresTurnstile(context))
        {
            await next();
            return;
        }

        var request = context.ActionArguments.Values.OfType<ITurnstileTokenRequest>().FirstOrDefault();
        if (request is null)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Title = "Turnstile verification failed",
                Detail = "Turnstile-protected endpoint request contract is missing.",
                Status = StatusCodes.Status400BadRequest,
                Extensions = { ["errorCode"] = "turnstile_contract_missing" }
            })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
            return;
        }

        var token = request?.TurnstileToken;
        var remoteIp = ResolveRequestIp(context.HttpContext.Request);
        var result = await turnstileVerifier.VerifyAsync(token, remoteIp, context.HttpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Title = "Turnstile verification failed",
                Detail = result.Message,
                Status = result.StatusCode,
                Extensions = { ["errorCode"] = result.ErrorCode }
            })
            {
                StatusCode = result.StatusCode
            };
            return;
        }

        await next();
    }

    private static bool RequiresTurnstile(FilterContext context)
        => context.ActionDescriptor.EndpointMetadata.OfType<RequireTurnstileAttribute>().Any();

    private static string? ResolveRequestIp(HttpRequest request)
    {
        var forwardedFor = request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var first = forwardedFor.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first))
            {
                return first;
            }
        }

        return request.HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}

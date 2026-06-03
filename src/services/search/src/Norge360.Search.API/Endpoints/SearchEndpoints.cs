// <copyright file="SearchEndpoints.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Norge360.Search.API.Security;
using Norge360.Search.Application.Abstractions;

namespace Norge360.Search.API.Endpoints;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/search", HandleSearchAsync)
            .AllowAnonymous();
        endpoints.MapGet("/api/v1/search/suggest", HandleSuggestAsync)
            .AllowAnonymous();

        return endpoints;
    }

    public static async Task<IResult> HandleSearchAsync(
        HttpContext httpContext,
        ISearchAccessContextFactory accessContextFactory,
        ISearchQueryService searchQueryService,
        CancellationToken cancellationToken)
    {
        if (!SearchEndpointQueryParser.TryParse(httpContext.Request.Query, out var request, out var error))
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid query parameters.",
                Detail = error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        var accessContext = accessContextFactory.Create(httpContext.User);
        var response = await searchQueryService.SearchAsync(request, accessContext, cancellationToken);

        return Results.Ok(response);
    }

    public static async Task<IResult> HandleSuggestAsync(
        HttpContext httpContext,
        ISearchAccessContextFactory accessContextFactory,
        ISearchQueryService searchQueryService,
        CancellationToken cancellationToken)
    {
        if (!SearchEndpointQueryParser.TryParseSuggest(httpContext.Request.Query, out var request, out var error))
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid suggest query parameters.",
                Detail = error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        var accessContext = accessContextFactory.Create(httpContext.User);
        var response = await searchQueryService.SearchAsync(request, accessContext, cancellationToken);

        return Results.Ok(response);
    }
}

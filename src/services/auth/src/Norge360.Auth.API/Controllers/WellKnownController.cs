// <copyright file="WellKnownController.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Norge360.Auth.Application.Abstractions;
using Norge360.Auth.Application.Options;

namespace Norge360.Auth.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route(".well-known")]
public sealed class WellKnownController(
    IOptions<JwtOptions> jwtOptions,
    ITokenSigningKeyProvider tokenSigningKeyProvider) : ControllerBase
{
    private const string JwksPath = "/.well-known/jwks.json";

    [HttpGet("jwks.json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetJwks()
    {
        return Ok(tokenSigningKeyProvider.GetJwksDocument(jwtOptions.Value.Issuer));
    }

    [HttpGet("openid-configuration")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetOpenIdConfiguration()
    {
        return Ok(new Dictionary<string, object>
        {
            ["issuer"] = jwtOptions.Value.Issuer,
            ["jwks_uri"] = BuildJwksUri(),
            ["id_token_signing_alg_values_supported"] = new[] { "RS256" },
            ["response_types_supported"] = new[] { "token" },
            ["subject_types_supported"] = new[] { "public" },
            ["token_endpoint_auth_methods_supported"] = new[] { "none" },
            ["claims_supported"] = new[]
            {
                "sub",
                "unique_name",
                "email",
                "sid",
                "token_version",
                "jti",
                "auth_time",
                "role",
                "permission"
            }
        });
    }

    private string BuildJwksUri()
    {
        return UriHelper.BuildAbsolute(Request.Scheme, Request.Host, Request.PathBase, JwksPath);
    }
}

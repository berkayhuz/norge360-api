// <copyright file="ISearchAccessContextFactory.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Security.Claims;
using Norge360.Search.Application.Security;

namespace Norge360.Search.API.Security;

public interface ISearchAccessContextFactory
{
    SearchAccessContext Create(ClaimsPrincipal? principal);
}

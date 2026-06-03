// <copyright file="SearchDocumentVisibility.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Search.Contracts.Documents;

public enum SearchDocumentVisibility
{
    Public = 1,
    Authenticated = 2,
    Tenant = 3,
    Permission = 4
}

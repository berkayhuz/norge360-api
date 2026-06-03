// <copyright file="ISearchStaticTextLocalizer.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Search.Application.StaticDocuments;

public interface ISearchStaticTextLocalizer
{
    IReadOnlyCollection<string> SupportedLocales { get; }

    string ResolveRequired(string key, string locale);

    string? ResolveOptional(string? key, string locale);

    IReadOnlyCollection<string> ResolveKeywords(IReadOnlyCollection<string> keys, string locale);
}

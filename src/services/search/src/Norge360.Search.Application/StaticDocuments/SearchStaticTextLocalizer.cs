// <copyright file="SearchStaticTextLocalizer.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Text.Json;
using Norge360.Search.Application.Localization;

namespace Norge360.Search.Application.StaticDocuments;

public sealed class SearchStaticTextLocalizer : ISearchStaticTextLocalizer
{
    private const string MessagesPathEnvironmentVariable = "NORGE360_SEARCH_MESSAGES_PATH";
    private static readonly char[] KeywordDelimiters = [',', ';', '\n', '\r'];

    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _messagesByLocale;

    public SearchStaticTextLocalizer(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> messagesByLocale)
    {
        _messagesByLocale = NormalizeCatalog(messagesByLocale);
    }

    public IReadOnlyCollection<string> SupportedLocales => SearchLocale.SupportedStaticLocales;

    public static SearchStaticTextLocalizer CreateDefault() => new(LoadDefaultCatalog());

    public string ResolveRequired(string key, string locale)
    {
        if (TryResolve(key, locale, out var value))
        {
            return value;
        }

        throw new KeyNotFoundException(
            $"Static search translation key '{key}' is missing for locale '{locale}' and fallback locale '{SearchLocale.DefaultLocale}'.");
    }

    public string? ResolveOptional(string? key, string locale)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return TryResolve(key, locale, out var value) ? value : null;
    }

    public IReadOnlyCollection<string> ResolveKeywords(IReadOnlyCollection<string> keys, string locale)
    {
        if (keys.Count == 0)
        {
            return [];
        }

        return keys
            .SelectMany(key => SplitKeywords(ResolveOptional(key, locale)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private bool TryResolve(string key, string locale, out string value)
    {
        var canonicalLocale = SearchLocale.CanonicalizeStaticLocale(locale);
        if (TryResolveExact(key, canonicalLocale, out value))
        {
            return true;
        }

        if (!string.Equals(canonicalLocale, SearchLocale.DefaultLocale, StringComparison.Ordinal) &&
            TryResolveExact(key, SearchLocale.DefaultLocale, out value))
        {
            return true;
        }

        value = string.Empty;
        return false;
    }

    private bool TryResolveExact(string key, string locale, out string value)
    {
        if (_messagesByLocale.TryGetValue(locale, out var messages) &&
            messages.TryGetValue(key, out var rawValue) &&
            !string.IsNullOrWhiteSpace(rawValue))
        {
            value = rawValue.Trim();
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> NormalizeCatalog(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> messagesByLocale)
    {
        var normalized = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);

        foreach (var entry in messagesByLocale)
        {
            var locale = SearchLocale.CanonicalizeStaticLocale(entry.Key);
            normalized[locale] = entry.Value
                .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key))
                .ToDictionary(
                    kvp => kvp.Key.Trim(),
                    kvp => kvp.Value ?? string.Empty,
                    StringComparer.Ordinal);
        }

        return normalized;
    }

    private static IReadOnlyCollection<string> SplitKeywords(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split(KeywordDelimiters, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .Select(keyword => keyword.Trim())
            .ToArray();
    }

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> LoadDefaultCatalog()
    {
        var messageDirectory = ResolveMessageDirectory();
        return SearchLocale.SupportedStaticLocales.ToDictionary(
            locale => locale,
            locale => (IReadOnlyDictionary<string, string>)ReadMessageFile(messageDirectory, locale),
            StringComparer.Ordinal);
    }

    private static IReadOnlyDictionary<string, string> ReadMessageFile(string messageDirectory, string locale)
    {
        var fileName = locale switch
        {
            "en-US" => "en-US.json",
            "nb-NO" => "nb-NO.json",
            "da-DK" => "da-DK.json",
            "de-DE" => "de-DE.json",
            "sv-SE" => "sv-SE.json",
            _ => $"{locale}.json"
        };
        var filePath = Path.Combine(messageDirectory, fileName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                $"Static search message file '{fileName}' was not found in '{messageDirectory}'. " +
                $"Set {MessagesPathEnvironmentVariable} to a directory containing en-US.json, nb-NO.json, da-DK.json, de-DE.json and sv-SE.json.",
                filePath);
        }

        using var stream = File.OpenRead(filePath);
        var messages = JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
        if (messages is null)
        {
            throw new InvalidOperationException($"Static search message file '{filePath}' is empty or invalid.");
        }

        return messages;
    }

    private static string ResolveMessageDirectory()
    {
        var configured = Environment.GetEnvironmentVariable(MessagesPathEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configured) && ContainsRequiredMessageFiles(configured))
        {
            return Path.GetFullPath(configured);
        }

        foreach (var candidate in EnumerateCandidateMessageDirectories())
        {
            if (ContainsRequiredMessageFiles(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException(
            "Static search messages were not found. Include the search application messages folder " +
            $"in the runtime artifact or set {MessagesPathEnvironmentVariable} to a directory containing en-US.json, nb-NO.json, da-DK.json, de-DE.json and sv-SE.json.");
    }

    private static IEnumerable<string> EnumerateCandidateMessageDirectories()
    {
        var roots = new[]
        {
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory()
        };

        foreach (var root in roots)
        {
            foreach (var directory in EnumerateParentDirectories(root))
            {
                yield return Path.Combine(directory, "messages");
                yield return Path.Combine(directory, "StaticDocuments", "messages");
            }
        }
    }

    private static IEnumerable<string> EnumerateParentDirectories(string root)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(root));
        while (directory is not null)
        {
            yield return directory.FullName;
            directory = directory.Parent;
        }
    }

    private static bool ContainsRequiredMessageFiles(string directory) =>
        File.Exists(Path.Combine(directory, "en-US.json")) &&
        File.Exists(Path.Combine(directory, "nb-NO.json")) &&
        File.Exists(Path.Combine(directory, "da-DK.json")) &&
        File.Exists(Path.Combine(directory, "de-DE.json")) &&
        File.Exists(Path.Combine(directory, "sv-SE.json"));
}

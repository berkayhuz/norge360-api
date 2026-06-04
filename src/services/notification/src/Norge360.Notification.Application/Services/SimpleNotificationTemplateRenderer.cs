// <copyright file="SimpleNotificationTemplateRenderer.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Net;
using System.Text.RegularExpressions;
using Norge360.Localization;
using Norge360.Notification.Application.Abstractions;
using Norge360.Notification.Contracts.Notifications.Requests;

namespace Norge360.Notification.Application.Services;

public sealed partial class SimpleNotificationTemplateRenderer : INotificationTemplateRenderer
{
    public SendNotificationRequest Render(SendNotificationRequest request)
    {
        var values = request.Metadata;
        var culture = ResolveCulture(values);
        var template = LocalizedNotificationTemplates.Resolve(request.TemplateKey, culture);
        var subject = Bind(template?.Subject ?? request.Subject, values, htmlEncode: false);
        var textBody = Bind(template?.TextBody ?? request.TextBody, values, htmlEncode: false);
        var htmlBody = template?.HtmlBody is null && request.HtmlBody is null
            ? null
            : Bind(template?.HtmlBody ?? request.HtmlBody!, values, htmlEncode: true);

        return request with
        {
            Subject = subject,
            TextBody = textBody,
            HtmlBody = htmlBody
        };
    }

    private static string Bind(string template, IReadOnlyDictionary<string, string> values, bool htmlEncode) =>
        PlaceholderRegex().Replace(template, match =>
        {
            var key = match.Groups["key"].Value;
            if (!values.TryGetValue(key, out var value))
            {
                return match.Value;
            }

            return htmlEncode
                ? WebUtility.HtmlEncode(value)
                : value;
        });

    [GeneratedRegex("\\{\\{(?<key>[a-zA-Z0-9_.-]+)\\}\\}", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderRegex();

    private static string ResolveCulture(IReadOnlyDictionary<string, string> metadata)
        => metadata.TryGetValue("culture", out var culture) || metadata.TryGetValue("Culture", out culture)
            ? Norge360Cultures.NormalizeOrDefault(culture)
            : Norge360Cultures.DefaultCulture;
}

internal sealed record LocalizedNotificationTemplate(string Subject, string TextBody, string HtmlBody);

internal static class LocalizedNotificationTemplates
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, LocalizedNotificationTemplate>> Templates =
        new Dictionary<string, IReadOnlyDictionary<string, LocalizedNotificationTemplate>>(StringComparer.OrdinalIgnoreCase)
        {
            ["auth.email-confirmation"] = Pair(
                english: new(
                    "Confirm your Norge360 email",
                    "Hello {{DisplayName}}, confirm your email by opening {{ActionUrl}}.",
                    "<p>Hello {{DisplayName}},</p><p>Confirm your email by opening <a href=\"{{ActionUrl}}\">this link</a>.</p>"),
                norwegian: new(
                    "Bekreft e-posten din hos Norge360",
                    "Hei {{DisplayName}}, bekreft e-posten din ved a apne {{ActionUrl}}.",
                    "<p>Hei {{DisplayName}},</p><p>Bekreft e-posten din ved a apne <a href=\"{{ActionUrl}}\">denne lenken</a>.</p>"),
                danish: new(
                    "Bekraeft din Norge360-e-mail",
                    "Hej {{DisplayName}}, bekraeft din e-mail ved at abne {{ActionUrl}}.",
                    "<p>Hej {{DisplayName}},</p><p>Bekraeft din e-mail ved at abne <a href=\"{{ActionUrl}}\">dette link</a>.</p>"),
                german: new(
                    "Bestaetige deine Norge360-E-Mail",
                    "Hallo {{DisplayName}}, bestaetige deine E-Mail, indem du {{ActionUrl}} oeffnest.",
                    "<p>Hallo {{DisplayName}},</p><p>Bestaetige deine E-Mail, indem du <a href=\"{{ActionUrl}}\">diesen Link</a> oeffnest.</p>"),
                swedish: new(
                    "Bekrafta din Norge360-e-post",
                    "Hej {{DisplayName}}, bekrafta din e-post genom att oppna {{ActionUrl}}.",
                    "<p>Hej {{DisplayName}},</p><p>Bekrafta din e-post genom att oppna <a href=\"{{ActionUrl}}\">den har lank</a>.</p>")),
            ["auth.password-reset"] = Pair(
                english: new(
                    "Reset your Norge360 password",
                    "Reset your password by opening {{ActionUrl}}.",
                    "<p>Reset your password by opening <a href=\"{{ActionUrl}}\">this link</a>.</p>"),
                norwegian: new(
                    "Tilbakestill Norge360-passordet ditt",
                    "Tilbakestill passordet ditt ved a apne {{ActionUrl}}.",
                    "<p>Tilbakestill passordet ditt ved a apne <a href=\"{{ActionUrl}}\">denne lenken</a>.</p>"),
                danish: new(
                    "Nulstil din Norge360-adgangskode",
                    "Nulstil din adgangskode ved at abne {{ActionUrl}}.",
                    "<p>Nulstil din adgangskode ved at abne <a href=\"{{ActionUrl}}\">dette link</a>.</p>"),
                german: new(
                    "Setze dein Norge360-Passwort zurueck",
                    "Setze dein Passwort zurueck, indem du {{ActionUrl}} oeffnest.",
                    "<p>Setze dein Passwort zurueck, indem du <a href=\"{{ActionUrl}}\">diesen Link</a> oeffnest.</p>"),
                swedish: new(
                    "Aterstall ditt Norge360-losenord",
                    "Aterstall ditt losenord genom att oppna {{ActionUrl}}.",
                    "<p>Aterstall ditt losenord genom att oppna <a href=\"{{ActionUrl}}\">den har lank</a>.</p>")),
            ["auth.email-change-confirmation"] = Pair(
                english: new(
                    "Confirm your new email address",
                    "Confirm this email change by opening {{ActionUrl}}.",
                    "<p>Confirm this email change by opening <a href=\"{{ActionUrl}}\">this link</a>.</p>"),
                norwegian: new(
                    "Bekreft den nye e-postadressen din",
                    "Bekreft denne endringen ved a apne {{ActionUrl}}.",
                    "<p>Bekreft denne endringen ved a apne <a href=\"{{ActionUrl}}\">denne lenken</a>.</p>"),
                danish: new(
                    "Bekraeft din nye e-mailadresse",
                    "Bekraeft denne e-mailaendring ved at abne {{ActionUrl}}.",
                    "<p>Bekraeft denne e-mailaendring ved at abne <a href=\"{{ActionUrl}}\">dette link</a>.</p>"),
                german: new(
                    "Bestaetige deine neue E-Mail-Adresse",
                    "Bestaetige diese E-Mail-Aenderung, indem du {{ActionUrl}} oeffnest.",
                    "<p>Bestaetige diese E-Mail-Aenderung, indem du <a href=\"{{ActionUrl}}\">diesen Link</a> oeffnest.</p>"),
                swedish: new(
                    "Bekrafta din nya e-postadress",
                    "Bekrafta denna andring genom att oppna {{ActionUrl}}.",
                    "<p>Bekrafta denna andring genom att oppna <a href=\"{{ActionUrl}}\">den har lank</a>.</p>")),
            ["account.security"] = Pair(
                english: new(
                    "Norge360 security notification",
                    "A security event occurred on your account: {{Reason}}.",
                    "<p>A security event occurred on your account: {{Reason}}.</p>"),
                norwegian: new(
                    "Norge360 sikkerhetsvarsel",
                    "En sikkerhetshendelse inntraff pa kontoen din: {{Reason}}.",
                    "<p>En sikkerhetshendelse inntraff pa kontoen din: {{Reason}}.</p>"),
                danish: new(
                    "Norge360 sikkerhedsnotifikation",
                    "Der skete en sikkerhedshandelse pa din konto: {{Reason}}.",
                    "<p>Der skete en sikkerhedshandelse pa din konto: {{Reason}}.</p>"),
                german: new(
                    "Norge360 Sicherheitsbenachrichtigung",
                    "Auf deinem Konto ist ein Sicherheitsereignis aufgetreten: {{Reason}}.",
                    "<p>Auf deinem Konto ist ein Sicherheitsereignis aufgetreten: {{Reason}}.</p>"),
                swedish: new(
                    "Norge360 sakerhetsnotis",
                    "En sakerhetshandelse intraffade pa ditt konto: {{Reason}}.",
                    "<p>En sakerhetshandelse intraffade pa ditt konto: {{Reason}}.</p>")),
            ["crm.reminder"] = Pair(
                english: new(
                    "CRM reminder",
                    "Reminder: {{Title}}.",
                    "<p>Reminder: {{Title}}.</p>"),
                norwegian: new(
                    "CRM-paminnelse",
                    "Paminnelse: {{Title}}.",
                    "<p>Paminnelse: {{Title}}.</p>"),
                danish: new(
                    "CRM-pamindelse",
                    "Pamindelse: {{Title}}.",
                    "<p>Pamindelse: {{Title}}.</p>"),
                german: new(
                    "CRM-Erinnerung",
                    "Erinnerung: {{Title}}.",
                    "<p>Erinnerung: {{Title}}.</p>"),
                swedish: new(
                    "CRM-paminnelse",
                    "Paminnelse: {{Title}}.",
                    "<p>Paminnelse: {{Title}}.</p>")),
            ["task.notification"] = Pair(
                english: new(
                    "Task notification",
                    "Task update: {{Title}}.",
                    "<p>Task update: {{Title}}.</p>"),
                norwegian: new(
                    "Oppgavevarsel",
                    "Oppgaveoppdatering: {{Title}}.",
                    "<p>Oppgaveoppdatering: {{Title}}.</p>"),
                danish: new(
                    "Opgavenotifikation",
                    "Opgaveopdatering: {{Title}}.",
                    "<p>Opgaveopdatering: {{Title}}.</p>"),
                german: new(
                    "Aufgabenbenachrichtigung",
                    "Aufgabenaktualisierung: {{Title}}.",
                    "<p>Aufgabenaktualisierung: {{Title}}.</p>"),
                swedish: new(
                    "Uppgiftsnotis",
                    "Uppgiftsuppdatering: {{Title}}.",
                    "<p>Uppgiftsuppdatering: {{Title}}.</p>"))
        };

    public static LocalizedNotificationTemplate? Resolve(string? templateKey, string culture)
        => templateKey is not null && Templates.TryGetValue(templateKey, out var localized)
            ? localized.TryGetValue(Norge360Cultures.NormalizeOrDefault(culture), out var template)
                ? template
                : localized[Norge360Cultures.DefaultCulture]
            : null;

    private static IReadOnlyDictionary<string, LocalizedNotificationTemplate> Pair(
        LocalizedNotificationTemplate english,
        LocalizedNotificationTemplate norwegian,
        LocalizedNotificationTemplate danish,
        LocalizedNotificationTemplate german,
        LocalizedNotificationTemplate swedish) =>
        new Dictionary<string, LocalizedNotificationTemplate>(StringComparer.OrdinalIgnoreCase)
        {
            [Norge360Cultures.EnglishCulture] = english,
            [Norge360Cultures.NorwegianBokmalCulture] = norwegian,
            [Norge360Cultures.DanishCulture] = danish,
            [Norge360Cultures.GermanCulture] = german,
            [Norge360Cultures.SwedishCulture] = swedish
        };
}

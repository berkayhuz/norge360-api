using System.Globalization;
using System.Security.Cryptography;
using System.Numerics;

namespace Norge360.Community.Domain.Utilities;

public static class PublicSlugGenerator
{
    public static string CreateNumericSlug()
    {
        Span<byte> buffer = stackalloc byte[8];
        RandomNumberGenerator.Fill(buffer);
        var raw = BitConverter.ToUInt64(buffer);
        var value = (raw % 9_000_000_000_000_000_000UL) + 1_000_000_000_000_000_000UL;
        return value.ToString(CultureInfo.InvariantCulture);
    }

    public static bool TryDecodePublicGuidSlug(string slug, out Guid guid)
    {
        guid = Guid.Empty;
        if (string.IsNullOrWhiteSpace(slug) || slug.Length > 39 || !BigInteger.TryParse(slug, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value < BigInteger.Zero)
        {
            return false;
        }

        var hex = value.ToString("x", CultureInfo.InvariantCulture).PadLeft(32, '0');
        if (hex.Length > 32)
        {
            return false;
        }

        var formatted = string.Create(36, hex, static (span, source) =>
        {
            source.AsSpan(0, 8).CopyTo(span);
            span[8] = '-';
            source.AsSpan(8, 4).CopyTo(span[9..]);
            span[13] = '-';
            source.AsSpan(12, 4).CopyTo(span[14..]);
            span[18] = '-';
            source.AsSpan(16, 4).CopyTo(span[19..]);
            span[23] = '-';
            source.AsSpan(20, 12).CopyTo(span[24..]);
        });

        return Guid.TryParseExact(formatted, "D", out guid);
    }
}

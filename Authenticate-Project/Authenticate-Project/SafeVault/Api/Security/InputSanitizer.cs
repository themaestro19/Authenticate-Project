using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SafeVault.Api.Security;

public readonly record struct SanitizedString(string Value, bool WasModified);

public static class InputSanitizer
{
    private static readonly Regex ControlChars = new(@"[\u0000-\u001F\u007F]", RegexOptions.Compiled);

    // Very strict allow-lists (change per your business rules)
    private static readonly Regex UserNameAllowed = new(@"^[a-zA-Z0-9_]{3,32}$", RegexOptions.Compiled);
    private static readonly Regex CurrencyAllowed = new(@"^[A-Z]{3}$", RegexOptions.Compiled);

    public static SanitizedString SanitizeUserName(string? input)
    {
        var normalized = NormalizeCommon(input);
        if (normalized.Length == 0) return new("", input is not null);

        // Remove anything not alnum/_ (requirement: "remove malicious characters")
        var stripped = Regex.Replace(normalized, @"[^a-zA-Z0-9_]", "");
        return new(stripped, !string.Equals(input, stripped, StringComparison.Ordinal));
    }

    public static SanitizedString SanitizeRecordName(string? input)
    {
        var normalized = NormalizeCommon(input);
        if (normalized.Length == 0) return new("", input is not null);

        // Allow letters/digits/space/_/-
        var stripped = Regex.Replace(normalized, @"[^a-zA-Z0-9 _\-]", "");
        stripped = Regex.Replace(stripped, @"\s+", " ").Trim();
        return new(stripped, !string.Equals(input, stripped, StringComparison.Ordinal));
    }

    public static SanitizedString SanitizeNotes(string? input)
    {
        var normalized = NormalizeCommon(input);
        if (normalized.Length == 0) return new("", input is not null);

        // Notes: keep text, but remove control chars; reject HTML-ish angle brackets by stripping
        var stripped = normalized.Replace("<", "").Replace(">", "");
        return new(stripped, !string.Equals(input, stripped, StringComparison.Ordinal));
    }

    public static SanitizedString SanitizeCurrency(string? input)
    {
        var normalized = NormalizeCommon(input).ToUpperInvariant();
        var stripped = Regex.Replace(normalized, @"[^A-Z]", "");
        return new(stripped, !string.Equals(input, stripped, StringComparison.OrdinalIgnoreCase));
    }

    public static void ValidateUserNameOrThrow(string userName)
    {
        if (!UserNameAllowed.IsMatch(userName))
            throw new ValidationException("Invalid username format.");
    }

    public static void ValidateCurrencyOrThrow(string currency)
    {
        if (!CurrencyAllowed.IsMatch(currency))
            throw new ValidationException("Invalid currency format (expected ISO-like 3 letters).");
    }

    private static string NormalizeCommon(string? input)
    {
        if (input is null) return "";
        var s = input.Trim();

        // Unicode normalization helps reduce “look-alike”/odd encodings
        s = s.Normalize(NormalizationForm.FormKC);

        // Remove control characters
        s = ControlChars.Replace(s, "");

        // Bound length early (defense-in-depth against abusive payload sizes)
        if (s.Length > 2048) s = s[..2048];

        return s;
    }
}

public sealed class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

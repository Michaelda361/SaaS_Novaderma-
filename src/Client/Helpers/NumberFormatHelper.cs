using System;
using System.Globalization;
using System.Linq;

namespace TalentManagement.Client.Helpers;

public static class NumberFormatHelper
{
    public static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var normalized = new string(value.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
        if (string.IsNullOrWhiteSpace(normalized)) return null;

        var clean = normalized.Replace(".", "");
        if (clean.Contains(','))
        {
            clean = clean.Replace(',', '.');
        }

        if (decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }
        return null;
    }

    public static string FormatDecimal(decimal? value)
    {
        if (value == null) return string.Empty;

        var val = value.Value;
        if (val % 1 == 0)
        {
            return val.ToString("N0", new CultureInfo("es-CO"));
        }
        else
        {
            return val.ToString("N2", new CultureInfo("es-CO"));
        }
    }

    public static string? CleanDynamicNumber(string? value)
    {
        var parsed = ParseDecimal(value);
        return parsed?.ToString(CultureInfo.InvariantCulture);
    }

    public static string FormatDynamicNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return FormatDecimal(parsed);
        }
        var parsedFallback = ParseDecimal(value);
        return parsedFallback != null ? FormatDecimal(parsedFallback) : value;
    }
}

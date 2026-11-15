using System;
using System.Collections.Generic;
using System.Globalization;

namespace EnjoyEveryday.UI.Shared.Services;

public class CurrencyService : ICurrencyService
{
    private static readonly Dictionary<string, CurrencyMetadata> Currencies = new(StringComparer.OrdinalIgnoreCase)
    {
        { "INR", new("INR", "₹", "Indian Rupee", "en-IN", 0) },
        { "JPY", new("JPY", "¥", "Japanese Yen", "ja-JP", 0) },
        { "USD", new("USD", "$", "US Dollar", "en-US", 2) },
        { "CAD", new("CAD", "$", "Canadian Dollar", "en-CA", 2) },
        { "EUR", new("EUR", "€", "Euro", "en-IE", 2) },
        { "GBP", new("GBP", "£", "British Pound", "en-GB", 2) },
        { "AUD", new("AUD", "$", "Australian Dollar", "en-AU", 2) },
        { "SGD", new("SGD", "$", "Singapore Dollar", "en-SG", 2) }
    };

    public string Format(decimal amount, string? currency = null, bool? forceDecimals = null)
    {
        var meta = Resolve(currency);
        var culture = CultureInfo.GetCultureInfo(meta.CultureCode);
        var decimals = forceDecimals.HasValue
            ? (forceDecimals.Value ? 2 : 0)
            : meta.DefaultDecimals;

        var formattedNumber = decimals == 0
            ? amount.ToString("N0", culture)
            : amount.ToString($"N{decimals}", culture);

        return $"{meta.Symbol}{formattedNumber}";
    }

    public string GetSymbol(string? currency = null)
    {
        return Resolve(currency).Symbol;
    }

    public string GetDisplayName(string? currency = null)
    {
        var meta = Resolve(currency);
        return $"{meta.Code} — {meta.DisplayName} ({meta.Symbol})";
    }

    private static CurrencyMetadata Resolve(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return Currencies["INR"]; // Default: INR
        }

        var normalized = currency.Trim().ToUpperInvariant();
        if (Currencies.TryGetValue(normalized, out var meta))
        {
            return meta;
        }

        // Generic fallback for any other ISO currency
        return new CurrencyMetadata(normalized, normalized + " ", normalized, "en-US", 2);
    }

    private sealed record CurrencyMetadata(
        string Code, 
        string Symbol, 
        string DisplayName, 
        string CultureCode, 
        int DefaultDecimals);
}

public static class CurrencyExtensions
{
    private static readonly CurrencyService _defaultService = new();

    public static string ToCurrency(this decimal amount, string? currency = null, bool? forceDecimals = null)
    {
        return _defaultService.Format(amount, currency, forceDecimals);
    }
}

using System;

namespace EnjoyEveryday.UI.Shared.Services;

public interface ICurrencyService
{
    string Format(decimal amount, string? currency = null, bool? forceDecimals = null);
    string GetSymbol(string? currency = null);
    string GetDisplayName(string? currency = null);
}


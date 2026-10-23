using Chubb.Claims.Application.Common.Interfaces;

namespace Chubb.Claims.Infrastructure.Services;

public sealed class ApacFxRateService : IFxRateService
{
    // Usually these rates would fetched from other service as broker, but for the purpose of this exercise, we will hardcode them here.
    private static readonly Dictionary<string, decimal> UsdRates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SGD"] = 0.74m,       // Singapore Dollar -> USD
        ["HKD"] = 0.128m,      // Hong Kong Dollar -> USD
        ["AUD"] = 0.66m,       // Australian Dollar -> USD
        ["MYR"] = 0.23m,       // Malaysian Ringgit -> USD
        ["THB"] = 0.029m,      // Thai Baht -> USD
        ["IDR"] = 0.000064m,   // Indonesian Rupiah -> USD
        ["USD"] = 1.00m
    };

    public decimal GetUsdRate(string currencyCode)
    {
        return UsdRates.TryGetValue(currencyCode, out var rate) ? rate : 1.00m;
    }

    public decimal ConvertToUsd(decimal nativeAmount, string currencyCode)
    {
        var rate = GetUsdRate(currencyCode);
        return decimal.Round(nativeAmount * rate, 2);
    }

    public IReadOnlyDictionary<string, decimal> GetAllRates() => UsdRates;
}

namespace Chubb.Claims.Application.Common.Interfaces;

public interface IFxRateService
{
    decimal GetUsdRate(string currencyCode);
    decimal ConvertToUsd(decimal nativeAmount, string currencyCode);
    IReadOnlyDictionary<string, decimal> GetAllRates();
}

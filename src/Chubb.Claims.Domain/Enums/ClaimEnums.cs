namespace Chubb.Claims.Domain.Enums;

public enum MarketCode
{
    SG, // Singapore (SGD)
    HK, // Hong Kong (HKD)
    AU, // Australia (AUD)
    MY, // Malaysia (MYR)
    TH, // Thailand (THB)
    ID  // Indonesia (IDR)
}

public enum ClaimType
{
    Motor,
    Property
}

public enum ClaimStatus
{
    Submitted,
    UnderReview,
    InfoRequested,
    Settled,
    Rejected
}

public static class MarketMetadata
{
    public static string GetCurrency(MarketCode market) => market switch
    {
        MarketCode.SG => "SGD",
        MarketCode.HK => "HKD",
        MarketCode.AU => "AUD",
        MarketCode.MY => "MYR",
        MarketCode.TH => "THB",
        MarketCode.ID => "IDR",
        _ => "USD"
    };

    public static string GetCountryName(MarketCode market) => market switch
    {
        MarketCode.SG => "Singapore",
        MarketCode.HK => "Hong Kong",
        MarketCode.AU => "Australia",
        MarketCode.MY => "Malaysia",
        MarketCode.TH => "Thailand",
        MarketCode.ID => "Indonesia",
        _ => market.ToString()
    };
}

using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Common.Interfaces;
using Chubb.Claims.Application.Contracts.Requests;
using Chubb.Claims.Application.Contracts.Responses;
using Chubb.Claims.Domain.Enums;

namespace Chubb.Claims.Application.Features.Metadata.Queries;

public sealed class GetBootstrapMetadataHandler(IFxRateService fxService)
    : IQueryHandler<GetBootstrapMetadataRequest, BootstrapMetadataResponse>
{
    public Task<BootstrapMetadataResponse> HandleAsync(GetBootstrapMetadataRequest query, CancellationToken cancellationToken = default)
    {
        var markets = Enum.GetValues<MarketCode>().Select(m =>
        {
            var currency = MarketMetadata.GetCurrency(m);
            return new MarketMetadataResponse(
                m.ToString(),
                MarketMetadata.GetCountryName(m),
                currency,
                fxService.GetUsdRate(currency));
        }).ToList();

        var personas = new List<PersonaMetadataResponse>
        {
            new("clm-tan", "Tan Wei Ming", "Claimant", "SG", "Policyholder (Singapore - Motor & Property)", "weiming.tan@example.sg", "+65 9123 4567"),
            new("clm-chloe", "Chloe Chan", "Claimant", "HK", "Policyholder (Hong Kong - Commercial Property)", "chloe.chan@example.hk", "+852 9388 1120"),
            new("clm-liam", "Liam O'Connor", "Claimant", "AU", "Policyholder (Australia - Motor & Property)", "liam.oconnor@example.com.au", "+61 412 889 301"),
            new("off-sarah", "Sarah Jenkins", "ClaimsOfficer", "SG, HK", "Senior Claims Adjuster (SG & HK)", "sarah.jenkins@chubb.com", "+65 6333 8800"),
            new("off-raj", "Rajesh Patel", "ClaimsOfficer", "AU, MY", "Claims Specialist (AU & MY)", "rajesh.patel@chubb.com", "+61 2 9273 0100"),
            new("off-narong", "Narong Srisai", "ClaimsOfficer", "TH, ID", "Regional Claims Officer (TH & ID)", "narong.srisai@chubb.com", "+66 2 611 4000"),
            new("mgr-victoria", "Victoria Lim", "ClaimsManager", "APAC (All 6 Markets)", "VP, APAC Claims Operations & Exposure", "victoria.lim@chubb.com", "+65 6333 8000")
        };

        var response = new BootstrapMetadataResponse(
            markets,
            Enum.GetNames<ClaimType>(),
            Enum.GetNames<ClaimStatus>(),
            personas);

        return Task.FromResult(response);
    }
}

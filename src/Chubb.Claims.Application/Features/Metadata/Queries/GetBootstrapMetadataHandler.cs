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
            new("clm-tan", "Lim Hean Soon", "Claimant", "SG", "Customer / Policyholder", "weiming.tan@example.sg", "+65 9123 4567"),
            new("off-sarah", "Admin Officer", "ClaimsOfficer", "APAC", "Claims Officer (Insurance Staff)", "sarah.jenkins@chubb.com", "+65 6333 8800")
        };

        var response = new BootstrapMetadataResponse(
            markets,
            Enum.GetNames<ClaimType>(),
            Enum.GetNames<ClaimStatus>(),
            personas);

        return Task.FromResult(response);
    }
}

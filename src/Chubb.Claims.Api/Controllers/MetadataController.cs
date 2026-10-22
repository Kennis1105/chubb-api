using Chubb.Claims.Application.Common.Interfaces;
using Chubb.Claims.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Chubb.Claims.Api.Controllers;

[ApiController]
[Route("api/metadata")]
[Produces("application/json")]
public sealed class MetadataController(IFxRateService fxService) : ControllerBase
{
    [HttpGet("bootstrap")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetBootstrapMetadata()
    {
        var markets = Enum.GetValues<MarketCode>().Select(m =>
        {
            var currency = MarketMetadata.GetCurrency(m);
            return new
            {
                code = m.ToString(),
                countryName = MarketMetadata.GetCountryName(m),
                currency,
                fxRateToUsd = fxService.GetUsdRate(currency)
            };
        });

        var personas = new object[]
        {
            new { id = "clm-tan", name = "Tan Wei Ming", role = "Claimant", market = "SG", title = "Policyholder (Singapore - Motor & Property)", email = "weiming.tan@example.sg", phone = "+65 9123 4567" },
            new { id = "clm-chloe", name = "Chloe Chan", role = "Claimant", market = "HK", title = "Policyholder (Hong Kong - Commercial Property)", email = "chloe.chan@example.hk", phone = "+852 9388 1120" },
            new { id = "clm-liam", name = "Liam O'Connor", role = "Claimant", market = "AU", title = "Policyholder (Australia - Motor & Property)", email = "liam.oconnor@example.com.au", phone = "+61 412 889 301" },
            new { id = "off-sarah", name = "Sarah Jenkins", role = "ClaimsOfficer", market = "SG, HK", title = "Senior Claims Adjuster (SG & HK)", email = "sarah.jenkins@chubb.com", phone = "+65 6333 8800" },
            new { id = "off-raj", name = "Rajesh Patel", role = "ClaimsOfficer", market = "AU, MY", title = "Claims Specialist (AU & MY)", email = "rajesh.patel@chubb.com", phone = "+61 2 9273 0100" },
            new { id = "off-narong", name = "Narong Srisai", role = "ClaimsOfficer", market = "TH, ID", title = "Regional Claims Officer (TH & ID)", email = "narong.srisai@chubb.com", phone = "+66 2 611 4000" },
            new { id = "mgr-victoria", name = "Victoria Lim", role = "ClaimsManager", market = "APAC (All 6 Markets)", title = "VP, APAC Claims Operations & Exposure", email = "victoria.lim@chubb.com", phone = "+65 6333 8000" }
        };

        return Ok(new
        {
            markets,
            claimTypes = Enum.GetNames<ClaimType>(),
            claimStatuses = Enum.GetNames<ClaimStatus>(),
            personas
        });
    }
}

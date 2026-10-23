using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Contracts.Requests;
using Chubb.Claims.Application.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Chubb.Claims.Api.Controllers;

// Get the summary report of the claims
[ApiController]
[Route("api/analytics")]
[Produces("application/json")]
public sealed class AnalyticsController(IDispatcher dispatcher) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ExposureAnalyticsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var result = await dispatcher.QueryAsync(new GetExposureAnalyticsRequest(), ct);
        return Ok(result);
    }
}

using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Contracts.Requests;
using Chubb.Claims.Application.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Chubb.Claims.Api.Controllers;

[ApiController]
[Route("api/analytics")]
[Produces("application/json")]
public sealed class AnalyticsController(IDispatcher dispatcher) : ControllerBase
{
    [HttpGet("exposure")]
    [ProducesResponseType(typeof(ExposureAnalyticsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExposureAnalytics(CancellationToken ct)
    {
        var result = await dispatcher.QueryAsync(new GetExposureAnalyticsRequest(), ct);
        return Ok(result);
    }
}

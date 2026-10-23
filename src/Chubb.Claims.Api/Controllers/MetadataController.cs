using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Contracts.Requests;
using Chubb.Claims.Application.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Chubb.Claims.Api.Controllers;


// This is used to populate some of the initial data, like currency, claim types, claim statuses, and personas for the front-end application. It is not meant to be used for any other purpose.
[ApiController]
[Route("api/metadata")]
[Produces("application/json")]
public sealed class MetadataController(IDispatcher dispatcher) : ControllerBase
{
    [HttpGet("bootstrap")]
    [ProducesResponseType(typeof(BootstrapMetadataResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBootstrapMetadata(CancellationToken ct)
    {
        var result = await dispatcher.QueryAsync(new GetBootstrapMetadataRequest(), ct);
        return Ok(result);
    }
}

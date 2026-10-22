using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Contracts.Requests;
using Chubb.Claims.Application.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Chubb.Claims.Api.Controllers;

[ApiController]
[Route("api/claims")]
[Produces("application/json")]
public sealed class ClaimsController(IDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ClaimSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClaims([FromQuery] GetClaimsListRequest request, CancellationToken ct)
    {
        var result = await dispatcher.QueryAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClaimDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClaimById(Guid id, CancellationToken ct)
    {
        var (_, _, role) = ResolveActor("clm-tan", "Tan Wei Ming", "Claimant");
        var isStaffRole = !role.Equals("Claimant", StringComparison.OrdinalIgnoreCase);

        var result = await dispatcher.QueryAsync(new GetClaimByIdRequest(id, isStaffRole), ct);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ClaimDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitClaim([FromBody] SubmitClaimRequest request, CancellationToken ct)
    {
        var (actorId, actorName, _) = ResolveActor("clm-tan", "Tan Wei Ming", "Claimant");
        var enrichedRequest = request with
        {
            ClaimantId = !string.IsNullOrWhiteSpace(request.ClaimantId) ? request.ClaimantId : actorId,
            ClaimantName = !string.IsNullOrWhiteSpace(request.ClaimantName) ? request.ClaimantName : actorName
        };

        var created = await dispatcher.SendAsync(enrichedRequest, ct);
        return CreatedAtAction(nameof(GetClaimById), new { id = created.Id }, created);
    }

    [HttpPost("{id:guid}/pickup")]
    [ProducesResponseType(typeof(ClaimDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PickUpClaim(Guid id, [FromBody] PickUpClaimRequest? request, CancellationToken ct)
    {
        var (actorId, actorName, _) = ResolveActor("off-sarah", "Sarah Jenkins", "ClaimsOfficer");
        var command = (request ?? new PickUpClaimRequest()) with
        {
            ClaimId = id,
            OfficerId = !string.IsNullOrWhiteSpace(request?.OfficerId) ? request.OfficerId : actorId,
            OfficerName = !string.IsNullOrWhiteSpace(request?.OfficerName) ? request.OfficerName : actorName
        };

        var updated = await dispatcher.SendAsync(command, ct);
        return Ok(updated);
    }

    [HttpPut("{id:guid}/assessment")]
    [ProducesResponseType(typeof(ClaimDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateAssessment(Guid id, [FromBody] UpdateAssessmentRequest request, CancellationToken ct)
    {
        var (actorId, actorName, _) = ResolveActor("off-sarah", "Sarah Jenkins", "ClaimsOfficer");
        var command = request with
        {
            ClaimId = id,
            OfficerId = !string.IsNullOrWhiteSpace(request.OfficerId) ? request.OfficerId : actorId,
            OfficerName = !string.IsNullOrWhiteSpace(request.OfficerName) ? request.OfficerName : actorName
        };

        var updated = await dispatcher.SendAsync(command, ct);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/request-info")]
    [ProducesResponseType(typeof(ClaimDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestInfo(Guid id, [FromBody] RequestInfoRequest request, CancellationToken ct)
    {
        var (actorId, actorName, _) = ResolveActor("off-sarah", "Sarah Jenkins", "ClaimsOfficer");
        var command = request with
        {
            ClaimId = id,
            OfficerId = !string.IsNullOrWhiteSpace(request.OfficerId) ? request.OfficerId : actorId,
            OfficerName = !string.IsNullOrWhiteSpace(request.OfficerName) ? request.OfficerName : actorName
        };

        var updated = await dispatcher.SendAsync(command, ct);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/respond-info")]
    [ProducesResponseType(typeof(ClaimDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RespondInfo(Guid id, [FromBody] RespondInfoRequest request, CancellationToken ct)
    {
        var (actorId, actorName, _) = ResolveActor("clm-tan", "Tan Wei Ming", "Claimant");
        var command = request with
        {
            ClaimId = id,
            ClaimantId = !string.IsNullOrWhiteSpace(request.ClaimantId) ? request.ClaimantId : actorId,
            ClaimantName = !string.IsNullOrWhiteSpace(request.ClaimantName) ? request.ClaimantName : actorName
        };

        var updated = await dispatcher.SendAsync(command, ct);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/settle")]
    [ProducesResponseType(typeof(ClaimDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SettleClaim(Guid id, [FromBody] SettleClaimRequest request, CancellationToken ct)
    {
        var (actorId, actorName, _) = ResolveActor("off-sarah", "Sarah Jenkins", "ClaimsOfficer");
        var command = request with
        {
            ClaimId = id,
            OfficerId = !string.IsNullOrWhiteSpace(request.OfficerId) ? request.OfficerId : actorId,
            OfficerName = !string.IsNullOrWhiteSpace(request.OfficerName) ? request.OfficerName : actorName
        };

        var updated = await dispatcher.SendAsync(command, ct);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(ClaimDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RejectClaim(Guid id, [FromBody] RejectClaimRequest request, CancellationToken ct)
    {
        var (actorId, actorName, _) = ResolveActor("off-sarah", "Sarah Jenkins", "ClaimsOfficer");
        var command = request with
        {
            ClaimId = id,
            OfficerId = !string.IsNullOrWhiteSpace(request.OfficerId) ? request.OfficerId : actorId,
            OfficerName = !string.IsNullOrWhiteSpace(request.OfficerName) ? request.OfficerName : actorName
        };

        var updated = await dispatcher.SendAsync(command, ct);
        return Ok(updated);
    }

    private (string UserId, string UserName, string Role) ResolveActor(string defaultId, string defaultName, string defaultRole)
    {
        var id = Request.Headers["X-Demo-User-Id"].FirstOrDefault();
        var name = Request.Headers["X-Demo-User-Name"].FirstOrDefault();
        var role = Request.Headers["X-Demo-Role"].FirstOrDefault();
        return (
            !string.IsNullOrWhiteSpace(id) ? id : defaultId,
            !string.IsNullOrWhiteSpace(name) ? name : defaultName,
            !string.IsNullOrWhiteSpace(role) ? role : defaultRole
        );
    }
}

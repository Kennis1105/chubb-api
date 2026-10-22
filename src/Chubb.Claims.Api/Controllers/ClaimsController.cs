using Chubb.Claims.Api.Contracts;
using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.DTOs;
using Chubb.Claims.Application.Features.Claims.Commands;
using Chubb.Claims.Application.Features.Claims.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Chubb.Claims.Api.Controllers;

[ApiController]
[Route("api/claims")]
[Produces("application/json")]
public sealed class ClaimsController(IDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ClaimSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClaims(
        [FromQuery] string? market,
        [FromQuery] string? claimType,
        [FromQuery] string? status,
        [FromQuery] string? claimantId,
        [FromQuery] string? assignedOfficerId,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var result = await dispatcher.QueryAsync(
            new GetClaimsListQuery(market, claimType, status, claimantId, assignedOfficerId, search), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClaimDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClaimById(Guid id, CancellationToken ct)
    {
        var (_, _, role) = ResolveActor("clm-tan", "Tan Wei Ming", "Claimant");
        var isStaffRole = !role.Equals("Claimant", StringComparison.OrdinalIgnoreCase);

        var result = await dispatcher.QueryAsync(new GetClaimByIdQuery(id, isStaffRole), ct);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ClaimDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitClaim([FromBody] SubmitClaimRequest req, CancellationToken ct)
    {
        var (actorId, actorName, _) = ResolveActor("clm-tan", "Tan Wei Ming", "Claimant");
        var command = new SubmitClaimCommand(
            req.PolicyNumber,
            req.Market,
            req.ClaimType,
            !string.IsNullOrWhiteSpace(req.ClaimantId) ? req.ClaimantId : actorId,
            !string.IsNullOrWhiteSpace(req.ClaimantName) ? req.ClaimantName : actorName,
            !string.IsNullOrWhiteSpace(req.ClaimantEmail) ? req.ClaimantEmail : "policyholder@example.com",
            !string.IsNullOrWhiteSpace(req.ClaimantPhone) ? req.ClaimantPhone : "+65 9000 0000",
            req.IncidentDateUtc == default ? DateTime.UtcNow : req.IncidentDateUtc,
            req.IncidentLocation,
            req.IncidentDescription,
            req.AssetIdentifier,
            req.EstimatedLoss);

        var created = await dispatcher.SendAsync(command, ct);
        return CreatedAtAction(nameof(GetClaimById), new { id = created.Id }, created);
    }

    [HttpPost("{id:guid}/pickup")]
    [ProducesResponseType(typeof(ClaimDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PickUpClaim(Guid id, [FromBody] PickUpClaimRequest? req, CancellationToken ct)
    {
        var (actorId, actorName, _) = ResolveActor("off-sarah", "Sarah Jenkins", "ClaimsOfficer");
        var command = new PickUpClaimCommand(
            id,
            !string.IsNullOrWhiteSpace(req?.OfficerId) ? req.OfficerId : actorId,
            !string.IsNullOrWhiteSpace(req?.OfficerName) ? req.OfficerName : actorName);

        var updated = await dispatcher.SendAsync(command, ct);
        return Ok(updated);
    }

    [HttpPut("{id:guid}/assessment")]
    [ProducesResponseType(typeof(ClaimDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateAssessment(Guid id, [FromBody] UpdateAssessmentRequest req, CancellationToken ct)
    {
        var (actorId, actorName, _) = ResolveActor("off-sarah", "Sarah Jenkins", "ClaimsOfficer");
        var command = new UpdateClaimAssessmentCommand(
            id,
            req.AssessedReserve,
            req.InternalNote,
            !string.IsNullOrWhiteSpace(req.OfficerId) ? req.OfficerId : actorId,
            !string.IsNullOrWhiteSpace(req.OfficerName) ? req.OfficerName : actorName);

        var updated = await dispatcher.SendAsync(command, ct);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/request-info")]
    [ProducesResponseType(typeof(ClaimDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestInfo(Guid id, [FromBody] RequestInfoRequest req, CancellationToken ct)
    {
        var (actorId, actorName, _) = ResolveActor("off-sarah", "Sarah Jenkins", "ClaimsOfficer");
        var command = new RequestClaimInfoCommand(
            id,
            req.Question,
            !string.IsNullOrWhiteSpace(req.OfficerId) ? req.OfficerId : actorId,
            !string.IsNullOrWhiteSpace(req.OfficerName) ? req.OfficerName : actorName);

        var updated = await dispatcher.SendAsync(command, ct);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/respond-info")]
    [ProducesResponseType(typeof(ClaimDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RespondInfo(Guid id, [FromBody] RespondInfoRequest req, CancellationToken ct)
    {
        var (actorId, actorName, _) = ResolveActor("clm-tan", "Tan Wei Ming", "Claimant");
        var command = new RespondToClaimInfoCommand(
            id,
            req.Response,
            req.SupportingDocumentRef,
            !string.IsNullOrWhiteSpace(req.ClaimantId) ? req.ClaimantId : actorId,
            !string.IsNullOrWhiteSpace(req.ClaimantName) ? req.ClaimantName : actorName);

        var updated = await dispatcher.SendAsync(command, ct);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/settle")]
    [ProducesResponseType(typeof(ClaimDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SettleClaim(Guid id, [FromBody] SettleClaimRequest req, CancellationToken ct)
    {
        var (actorId, actorName, _) = ResolveActor("off-sarah", "Sarah Jenkins", "ClaimsOfficer");
        var command = new SettleClaimCommand(
            id,
            req.SettledAmount,
            req.DecisionNotes,
            !string.IsNullOrWhiteSpace(req.OfficerId) ? req.OfficerId : actorId,
            !string.IsNullOrWhiteSpace(req.OfficerName) ? req.OfficerName : actorName);

        var updated = await dispatcher.SendAsync(command, ct);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(ClaimDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RejectClaim(Guid id, [FromBody] RejectClaimRequest req, CancellationToken ct)
    {
        var (actorId, actorName, _) = ResolveActor("off-sarah", "Sarah Jenkins", "ClaimsOfficer");
        var command = new RejectClaimCommand(
            id,
            req.RejectionReason,
            !string.IsNullOrWhiteSpace(req.OfficerId) ? req.OfficerId : actorId,
            !string.IsNullOrWhiteSpace(req.OfficerName) ? req.OfficerName : actorName);

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

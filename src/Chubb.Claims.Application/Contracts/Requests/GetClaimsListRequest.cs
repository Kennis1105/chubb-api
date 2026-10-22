using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Contracts.Responses;

namespace Chubb.Claims.Application.Contracts.Requests;

public sealed record GetClaimsListRequest(
    string? Market = null,
    string? ClaimType = null,
    string? Status = null,
    string? ClaimantId = null,
    string? AssignedOfficerId = null,
    string? Search = null) : IQuery<IReadOnlyList<ClaimSummaryResponse>>;

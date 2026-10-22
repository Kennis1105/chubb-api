using System.Text.Json.Serialization;
using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Contracts.Responses;

namespace Chubb.Claims.Application.Contracts.Requests;

public sealed record RejectClaimRequest(
    string RejectionReason,
    string? OfficerId = null,
    string? OfficerName = null,
    [property: JsonIgnore] Guid ClaimId = default) : ICommand<ClaimDetailResponse>;

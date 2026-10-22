using System.Text.Json.Serialization;
using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Contracts.Responses;

namespace Chubb.Claims.Application.Contracts.Requests;

public sealed record RespondInfoRequest(
    string Response,
    string? SupportingDocumentRef = null,
    string? ClaimantId = null,
    string? ClaimantName = null,
    [property: JsonIgnore] Guid ClaimId = default) : ICommand<ClaimDetailResponse>;

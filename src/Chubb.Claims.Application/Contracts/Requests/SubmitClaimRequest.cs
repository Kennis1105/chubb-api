using System.Text.Json.Serialization;
using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Contracts.Responses;

namespace Chubb.Claims.Application.Contracts.Requests;

public sealed record SubmitClaimRequest(
    string PolicyNumber,
    string Market,
    string ClaimType,
    string? ClaimantId,
    string? ClaimantName,
    string? ClaimantEmail,
    string? ClaimantPhone,
    DateTime IncidentDateUtc,
    string IncidentLocation,
    string IncidentDescription,
    string AssetIdentifier,
    decimal EstimatedLoss) : ICommand<ClaimDetailResponse>;

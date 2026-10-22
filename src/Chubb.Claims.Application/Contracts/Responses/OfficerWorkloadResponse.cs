namespace Chubb.Claims.Application.Contracts.Responses;

public sealed record OfficerWorkloadResponse(
    string OfficerId,
    string OfficerName,
    string PrimaryMarkets,
    int ActiveUnderReviewCount,
    int AwaitingInfoCount,
    int SettledCount,
    int RejectedCount,
    decimal ActiveReserveManagedUsd,
    double AverageResolutionHours);

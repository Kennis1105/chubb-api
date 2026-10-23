using Chubb.Claims.Domain.Entities;
using Chubb.Claims.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Chubb.Claims.Infrastructure.Persistence;

public static class ClaimsDbSeeder
{
    public static async Task SeedAsync(ClaimsDbContext dbContext)
    {
        await dbContext.Database.EnsureCreatedAsync();

        if (await dbContext.Claims.AnyAsync())
            return;

        var now = DateTime.UtcNow;

        // 1. Incoming Unassigned Claim (Submitted) -> Ready for Claims Officer to Pick Up!
        var claimSubmitted = new Claim
        {
            Id = Guid.NewGuid(),
            ClaimNumber = "CHB-SG-2026-1001",
            PolicyNumber = "SG-MTR-884920",
            Market = MarketCode.SG,
            ClaimType = ClaimType.Motor,
            Status = ClaimStatus.Submitted,
            ClaimantId = "clm-tan",
            ClaimantName = "Lim Hean Soon",
            ClaimantEmail = "weiming.tan@example.sg",
            ClaimantPhone = "+65 9123 4567",
            IncidentDateUtc = now.AddDays(-1),
            IncidentLocation = "Pan Island Expressway (PIE), Singapore",
            IncidentDescription = "Rear-end collision during heavy rain. Rear bumper and parking sensors damaged.",
            AssetIdentifier = "SGP 8821 M (Mercedes-Benz C200)",
            Currency = "SGD",
            EstimatedLoss = 12000m,
            SubmittedAtUtc = now.AddDays(-1),
            UpdatedAtUtc = now.AddDays(-1)
        };

        // 2. Action Required Claim (InfoRequested) -> Ready for Claimant to Provide Additional Info!
        var claimInfoId = Guid.NewGuid();
        var claimInfoRequested = new Claim
        {
            Id = claimInfoId,
            ClaimNumber = "CHB-SG-2026-1002",
            PolicyNumber = "SG-PRP-510293",
            Market = MarketCode.SG,
            ClaimType = ClaimType.Property,
            Status = ClaimStatus.InfoRequested,
            ClaimantId = "clm-tan",
            ClaimantName = "Lim Hean Soon",
            ClaimantEmail = "weiming.tan@example.sg",
            ClaimantPhone = "+65 9123 4567",
            IncidentDateUtc = now.AddDays(-3),
            IncidentLocation = "18 Marina Boulevard #24-08, Singapore",
            IncidentDescription = "Burst water pipe in ceiling caused water damage to living room flooring.",
            AssetIdentifier = "18 Marina Blvd #24-08",
            Currency = "SGD",
            EstimatedLoss = 25000m,
            AssessedReserve = 25000m,
            AssignedOfficerId = "off-sarah",
            AssignedOfficerName = "Admin Officer",
            SubmittedAtUtc = now.AddDays(-3),
            UpdatedAtUtc = now.AddDays(-1),
            InformationRequests =
            [
                new ClaimInformationRequest
                {
                    ClaimId = claimInfoId,
                    Question = "Please provide the plumber's repair invoice and photos of the damaged flooring.",
                    RequestedByOfficerId = "off-sarah",
                    RequestedByOfficerName = "Admin Officer",
                    RequestedAtUtc = now.AddDays(-1)
                }
            ]
        };

        // 3. Under Review Claim (UnderReview) -> Ready for Officer to Approve or Reject!
        var claimUnderReview = new Claim
        {
            Id = Guid.NewGuid(),
            ClaimNumber = "CHB-HK-2026-1001",
            PolicyNumber = "HK-PRP-339104",
            Market = MarketCode.HK,
            ClaimType = ClaimType.Property,
            Status = ClaimStatus.UnderReview,
            ClaimantId = "clm-tan",
            ClaimantName = "Lim Hean Soon",
            ClaimantEmail = "weiming.tan@example.sg",
            ClaimantPhone = "+65 9123 4567",
            IncidentDateUtc = now.AddDays(-4),
            IncidentLocation = "88 Queensway, Admiralty, Hong Kong",
            IncidentDescription = "Typhoon wind damaged office window panel and server UPS unit.",
            AssetIdentifier = "Unit 1902, 88 Queensway, HK",
            Currency = "HKD",
            EstimatedLoss = 150000m,
            AssessedReserve = 150000m,
            AssignedOfficerId = "off-sarah",
            AssignedOfficerName = "Admin Officer",
            SubmittedAtUtc = now.AddDays(-4),
            UpdatedAtUtc = now.AddDays(-3)
        };

        // 4. Approved / Settled Claim (Settled) -> Shows Claimant & Officer a completed decision!
        var claimSettled = new Claim
        {
            Id = Guid.NewGuid(),
            ClaimNumber = "CHB-AU-2026-1001",
            PolicyNumber = "AU-MTR-772190",
            Market = MarketCode.AU,
            ClaimType = ClaimType.Motor,
            Status = ClaimStatus.Settled,
            ClaimantId = "clm-tan",
            ClaimantName = "Lim Hean Soon",
            ClaimantEmail = "weiming.tan@example.sg",
            ClaimantPhone = "+65 9123 4567",
            IncidentDateUtc = now.AddDays(-7),
            IncidentLocation = "George Street, Sydney NSW, Australia",
            IncidentDescription = "Hailstorm dent damage to vehicle bonnet and front windscreen.",
            AssetIdentifier = "NSW-CXR-94A (BMW X5)",
            Currency = "AUD",
            EstimatedLoss = 10000m,
            AssessedReserve = 10000m,
            SettledAmount = 9500m,
            AssignedOfficerId = "off-sarah",
            AssignedOfficerName = "Admin Officer",
            DecisionNotes = "Claim approved for full repair cost less AUD 500 policy excess.",
            SubmittedAtUtc = now.AddDays(-7),
            UpdatedAtUtc = now.AddDays(-5),
            ClosedAtUtc = now.AddDays(-5)
        };

        dbContext.Claims.AddRange(
            claimSubmitted,
            claimInfoRequested,
            claimUnderReview,
            claimSettled);

        await dbContext.SaveChangesAsync();
    }
}

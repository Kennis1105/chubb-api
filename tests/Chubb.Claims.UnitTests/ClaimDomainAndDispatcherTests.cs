using Chubb.Claims.Application;
using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Contracts.Requests;
using Chubb.Claims.Domain.Common;
using Chubb.Claims.Domain.Entities;
using Chubb.Claims.Domain.Enums;
using Chubb.Claims.Domain.Events;
using Chubb.Claims.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Chubb.Claims.UnitTests;

public class ClaimDomainAndDispatcherTests
{
    [Fact]
    public void Submit_CreatesClaimInSubmittedStatus_WithInitialReserveExposureAndDomainEvent()
    {
        var claim = Claim.Submit(
            claimNumber: "CHB-SG-2026-1001",
            policyNumber: "SG-MTR-9001",
            market: MarketCode.SG,
            claimType: ClaimType.Motor,
            claimantId: "clm-tan",
            claimantName: "Tan Wei Ming",
            claimantEmail: "tan@example.sg",
            claimantPhone: "+65 9000 1111",
            incidentDateUtc: DateTime.UtcNow.AddDays(-1),
            incidentLocation: "Orchard Road, Singapore",
            incidentDescription: "Rear bumper collision.",
            assetIdentifier: "SGP 1234 A",
            estimatedLoss: 12000m);

        Assert.Equal(ClaimStatus.Submitted, claim.Status);
        Assert.Equal("SGD", claim.Currency);
        Assert.Equal(12000m, claim.EstimatedLoss);
        Assert.Null(claim.AssessedReserve);
        Assert.Equal(12000m, claim.ActiveReserveExposure);
        Assert.Equal(0m, claim.RealizedPayout);
        Assert.Single(claim.DomainEvents);
        Assert.IsType<ClaimSubmittedDomainEvent>(claim.DomainEvents.First());
    }

    [Fact]
    public void ReserveLifecycle_EvolvesFromEstimatedLoss_ToAssessedReserve_AndDropsToZeroOnSettle()
    {
        var claim = Claim.Submit(
            "CHB-AU-2026-1001", "AU-PRP-100", MarketCode.AU, ClaimType.Property,
            "clm-liam", "Liam", "liam@example.au", "+61 400", DateTime.UtcNow,
            "Sydney", "Hail damage to roof", "12 George St", 25000m);

        // 1. Officer picks up claim
        claim.PickUp("off-raj", "Rajesh Patel");
        Assert.Equal(ClaimStatus.UnderReview, claim.Status);
        Assert.Equal(25000m, claim.ActiveReserveExposure);

        // 2. Officer adjusts AssessedReserve upward after loss adjuster inspection
        claim.UpdateAssessment(32000m, "Roof truss requires replacement.", "off-raj", "Rajesh Patel");
        Assert.Equal(32000m, claim.AssessedReserve);
        Assert.Equal(32000m, claim.ActiveReserveExposure);

        // 3. Officer requests info -> Claimant responds -> returns to UnderReview
        claim.RequestAdditionalInfo("Upload builder invoice.", "off-raj", "Rajesh Patel");
        Assert.Equal(ClaimStatus.InfoRequested, claim.Status);
        Assert.Equal(32000m, claim.ActiveReserveExposure);

        claim.RespondToInformationRequest("Builder invoice #991 uploaded.", "INV-991.pdf", "clm-liam", "Liam");
        Assert.Equal(ClaimStatus.UnderReview, claim.Status);

        // 4. Officer settles claim -> ActiveReserveExposure drops to 0, RealizedPayout becomes SettledAmount
        claim.Settle(30500m, "Approved less AUD 1,500 policy excess.", "off-raj", "Rajesh Patel");
        Assert.Equal(ClaimStatus.Settled, claim.Status);
        Assert.Equal(0m, claim.ActiveReserveExposure);
        Assert.Equal(30500m, claim.RealizedPayout);
        Assert.Equal(0m, claim.ReleasedReserve);
    }

    [Fact]
    public void Reject_ReleasesActiveReserveExposure_AndPreventsFurtherMutations()
    {
        var claim = Claim.Submit(
            "CHB-HK-2026-1001", "HK-PRP-200", MarketCode.HK, ClaimType.Property,
            "clm-chloe", "Chloe", "chloe@example.hk", "+852 900", DateTime.UtcNow,
            "Central HK", "Water seepage", "Unit 8A", 100000m);

        claim.PickUp("off-sarah", "Sarah Jenkins");
        claim.Reject("Gradual wear and tear exclusion applies.", "off-sarah", "Sarah Jenkins");

        Assert.Equal(ClaimStatus.Rejected, claim.Status);
        Assert.Equal(0m, claim.ActiveReserveExposure);
        Assert.Equal(100000m, claim.ReleasedReserve);

        Assert.Throws<DomainException>(() =>
            claim.Settle(50000m, "Should fail", "off-sarah", "Sarah Jenkins"));
    }

    [Fact]
    public async Task Dispatcher_EndToEndWithSqliteOutbox_PersistsOutboxEventsAndMasksInternalNotesForClaimant()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContext<ClaimsDbContext>(opts => opts.UseSqlite(connection));
        services.AddApplicationLayer();
        services.AddScoped<Chubb.Claims.Application.Common.Interfaces.IClaimsRepository, ClaimsRepository>();
        services.AddSingleton<Chubb.Claims.Application.Common.Interfaces.IFxRateService, Chubb.Claims.Infrastructure.Services.ApacFxRateService>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();
        await db.Database.EnsureCreatedAsync();

        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

        // 1. Submit Claim via IDispatcher using SubmitClaimRequest
        var created = await dispatcher.SendAsync(new SubmitClaimRequest(
            PolicyNumber: "SG-MTR-777777",
            Market: "SG",
            ClaimType: "Motor",
            ClaimantId: "clm-tan",
            ClaimantName: "Tan Wei Ming",
            ClaimantEmail: "tan@example.sg",
            ClaimantPhone: "+65 9123 4567",
            IncidentDateUtc: DateTime.UtcNow,
            IncidentLocation: "ECP Highway, Singapore",
            IncidentDescription: "Side-swipe collision",
            AssetIdentifier: "SGP 9999 Z",
            EstimatedLoss: 10000m));

        Assert.Equal("CHB-SG-" + DateTime.UtcNow.Year + "-1001", created.ClaimNumber);
        Assert.Equal(7400m, created.ActiveReserveExposureUsd);

        // 2. Officer adds internal note & adjusts reserve via IDispatcher using UpdateAssessmentRequest
        await dispatcher.SendAsync(new UpdateAssessmentRequest(
            AssessedReserve: 15000m,
            InternalNote: "CONFIDENTIAL OFFICER NOTE: Fraud check cleared, workshop estimate verified.",
            OfficerId: "off-sarah",
            OfficerName: "Sarah Jenkins",
            ClaimId: created.Id));

        // 3. Query as Claimant -> Internal note MUST be masked
        var claimantView = await dispatcher.QueryAsync(new GetClaimByIdRequest(created.Id, IsStaffRole: false));
        Assert.Empty(claimantView.Notes);
        Assert.Equal(11100m, claimantView.ActiveReserveExposureUsd);

        // 4. Query as Staff -> Internal note MUST be visible
        var staffView = await dispatcher.QueryAsync(new GetClaimByIdRequest(created.Id, IsStaffRole: true));
        Assert.Single(staffView.Notes);
        Assert.Contains("CONFIDENTIAL OFFICER NOTE", staffView.Notes[0].Content);

        // 5. Verify Transactional Outbox messages were written atomically
        var outboxCount = await db.OutboxMessages.CountAsync();
        Assert.Equal(2, outboxCount);
    }
}

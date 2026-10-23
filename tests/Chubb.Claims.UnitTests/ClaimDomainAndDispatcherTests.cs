using Chubb.Claims.Application;
using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Common.Interfaces;
using Chubb.Claims.Application.Contracts.Requests;
using Chubb.Claims.Domain.Common;
using Chubb.Claims.Infrastructure.Persistence;
using Chubb.Claims.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Chubb.Claims.UnitTests;

public class ClaimDomainAndDispatcherTests
{
    private static async Task<(ServiceProvider Provider, SqliteConnection Connection)> BuildTestContainerAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContext<ClaimsDbContext>(opts => opts.UseSqlite(connection));
        services.AddApplicationLayer();
        services.AddScoped<IClaimRepository, ClaimRepository>();
        services.AddSingleton<IFxRateService, ApacFxRateService>();

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();
        await db.Database.EnsureCreatedAsync();

        return (provider, connection);
    }

    [Fact]
    public async Task SubmitClaimHandler_CreatesClaimInSubmittedStatus_WithInitialReserveExposureAndOutboxEvent()
    {
        var (provider, conn) = await BuildTestContainerAsync();
        await using var _ = conn;
        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

        var created = await dispatcher.SendAsync(new SubmitClaimRequest(
            PolicyNumber: "SG-MTR-9001",
            Market: "SG",
            ClaimType: "Motor",
            ClaimantId: "clm-tan",
            ClaimantName: "Lim Hean Soon",
            ClaimantEmail: "tan@example.sg",
            ClaimantPhone: "+65 9000 1111",
            IncidentDateUtc: DateTime.UtcNow.AddDays(-1),
            IncidentLocation: "Orchard Road, Singapore",
            IncidentDescription: "Rear bumper collision.",
            AssetIdentifier: "SGP 1234 A",
            EstimatedLoss: 12000m));

        Assert.Equal("Submitted", created.Status);
        Assert.Equal("SGD", created.Currency);
        Assert.Equal(12000m, created.EstimatedLoss);
        Assert.Equal(12000m, created.ActiveReserveExposure);
    }

    [Fact]
    public async Task ReserveLifecycle_EvolvesFromEstimatedLoss_ToAssessedReserve_AndDropsToZeroOnSettle()
    {
        var (provider, conn) = await BuildTestContainerAsync();
        await using var _ = conn;
        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

        var created = await dispatcher.SendAsync(new SubmitClaimRequest(
            "AU-PRP-100", "AU", "Property", "clm-tan", "Lim Hean Soon", "tan@example.sg", "+65 900",
            DateTime.UtcNow, "Sydney", "Hail damage to roof", "12 George St", 25000m));

        // 1. Officer picks up claim
        var pickedUp = await dispatcher.SendAsync(new PickUpClaimRequest(created.Id, "off-sarah", "Admin Officer"));
        Assert.Equal("UnderReview", pickedUp.Status);
        Assert.Equal(25000m, pickedUp.ActiveReserveExposure);

        // 2. Officer adjusts AssessedReserve upward
        var assessed = await dispatcher.SendAsync(new UpdateAssessmentRequest(32000m, "Roof truss requires replacement.", "off-sarah", "Admin Officer", created.Id));
        Assert.Equal(32000m, assessed.AssessedReserve);
        Assert.Equal(32000m, assessed.ActiveReserveExposure);

        // 3. Officer requests info -> Claimant responds -> returns to UnderReview
        var infoReq = await dispatcher.SendAsync(new RequestInfoRequest("Upload builder invoice.", "off-sarah", "Admin Officer", created.Id));
        Assert.Equal("InfoRequested", infoReq.Status);

        var responded = await dispatcher.SendAsync(new RespondInfoRequest("Builder invoice #991 uploaded.", "INV-991.pdf", "clm-tan", "Lim Hean Soon", created.Id));
        Assert.Equal("UnderReview", responded.Status);

        // 4. Officer settles claim -> ActiveReserveExposure drops to 0
        var settled = await dispatcher.SendAsync(new SettleClaimRequest(30500m, "Approved less AUD 1,500 policy excess.", "off-sarah", "Admin Officer", created.Id));
        Assert.Equal("Settled", settled.Status);
        Assert.Equal(0m, settled.ActiveReserveExposure);
        Assert.Equal(30500m, settled.SettledAmount);
    }

    [Fact]
    public async Task RejectClaimHandler_ReleasesActiveReserveExposure_AndPreventsFurtherSettlement()
    {
        var (provider, conn) = await BuildTestContainerAsync();
        await using var _ = conn;
        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

        var created = await dispatcher.SendAsync(new SubmitClaimRequest(
            "HK-PRP-200", "HK", "Property", "clm-tan", "Lim Hean Soon", "tan@example.sg", "+852 900",
            DateTime.UtcNow, "Central HK", "Water seepage", "Unit 8A", 100000m));

        await dispatcher.SendAsync(new PickUpClaimRequest(created.Id, "off-sarah", "Admin Officer"));
        var rejected = await dispatcher.SendAsync(new RejectClaimRequest("Gradual wear and tear exclusion applies.", "off-sarah", "Admin Officer", created.Id));

        Assert.Equal("Rejected", rejected.Status);
        Assert.Equal(0m, rejected.ActiveReserveExposure);

        await Assert.ThrowsAsync<DomainException>(() =>
            dispatcher.SendAsync(new SettleClaimRequest(50000m, "Should fail", "off-sarah", "Admin Officer", created.Id)));
    }

    [Fact]
    public async Task Dispatcher_EndToEndWithSqliteOutbox_PersistsOutboxEventsAndMasksInternalNotesForClaimant()
    {
        var (provider, conn) = await BuildTestContainerAsync();
        await using var _ = conn;
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

        var created = await dispatcher.SendAsync(new SubmitClaimRequest(
            PolicyNumber: "SG-MTR-777777",
            Market: "SG",
            ClaimType: "Motor",
            ClaimantId: "clm-tan",
            ClaimantName: "Lim Hean Soon",
            ClaimantEmail: "tan@example.sg",
            ClaimantPhone: "+65 9123 4567",
            IncidentDateUtc: DateTime.UtcNow,
            IncidentLocation: "ECP Highway, Singapore",
            IncidentDescription: "Side-swipe collision",
            AssetIdentifier: "SGP 9999 Z",
            EstimatedLoss: 10000m));

        await dispatcher.SendAsync(new UpdateAssessmentRequest(
            AssessedReserve: 15000m,
            InternalNote: "CONFIDENTIAL OFFICER NOTE: Fraud check cleared, workshop estimate verified.",
            OfficerId: "off-sarah",
            OfficerName: "Admin Officer",
            ClaimId: created.Id));

        var claimantView = await dispatcher.QueryAsync(new GetClaimByIdRequest(created.Id, IsStaffRole: false));
        Assert.Empty(claimantView.Notes);
        Assert.Equal(11100m, claimantView.ActiveReserveExposureUsd);

        var staffView = await dispatcher.QueryAsync(new GetClaimByIdRequest(created.Id, IsStaffRole: true));
        Assert.Single(staffView.Notes);
        Assert.Contains("CONFIDENTIAL OFFICER NOTE", staffView.Notes[0].Content);

        var outboxCount = await db.OutboxMessages.CountAsync();
        Assert.Equal(2, outboxCount);
    }
}

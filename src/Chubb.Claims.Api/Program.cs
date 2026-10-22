using Chubb.Claims.Api.Contracts;
using Chubb.Claims.Api.Hubs;
using Chubb.Claims.Api.Workers;
using Chubb.Claims.Application;
using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Common.Interfaces;
using Chubb.Claims.Application.Features.Claims.Commands;
using Chubb.Claims.Application.Features.Claims.Queries;
using Chubb.Claims.Domain.Common;
using Chubb.Claims.Domain.Enums;
using Chubb.Claims.Infrastructure;
using Chubb.Claims.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddApplicationLayer();
builder.Services.AddInfrastructureLayer(builder.Configuration);
builder.Services.AddHostedService<OutboxRelayWorker>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200", "http://localhost:8080")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Global Exception Handling returning clean RFC 7807 ProblemDetails
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var (statusCode, title) = exception switch
        {
            DomainException => (StatusCodes.Status400BadRequest, "Domain Rule Violation"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource Not Found"),
            _ => (StatusCodes.Status500InternalServerError, "Server Error")
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception?.Message ?? "An unexpected error occurred."
        });
    });
});

app.UseCors("AllowAngularDev");

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Chubb APAC Claims API v1");
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "Chubb APAC Claims API — Swagger UI";
});

// Auto-migrate & seed SQLite database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();
    await ClaimsDbSeeder.SeedAsync(db);
}

// Helper to resolve active persona from headers (`X-Demo-User-Id`, `X-Demo-User-Name`, `X-Demo-Role`)
static (string UserId, string UserName, string Role) ResolveActor(HttpContext http, string defaultId, string defaultName, string defaultRole)
{
    var id = http.Request.Headers["X-Demo-User-Id"].FirstOrDefault();
    var name = http.Request.Headers["X-Demo-User-Name"].FirstOrDefault();
    var role = http.Request.Headers["X-Demo-Role"].FirstOrDefault();
    return (
        !string.IsNullOrWhiteSpace(id) ? id : defaultId,
        !string.IsNullOrWhiteSpace(name) ? name : defaultName,
        !string.IsNullOrWhiteSpace(role) ? role : defaultRole
    );
}

var api = app.MapGroup("/api");

// Metadata & Personas endpoint for the single Angular codebase
api.MapGet("/metadata/bootstrap", (IFxRateService fxService) =>
{
    var markets = Enum.GetValues<MarketCode>().Select(m =>
    {
        var currency = MarketMetadata.GetCurrency(m);
        return new
        {
            code = m.ToString(),
            countryName = MarketMetadata.GetCountryName(m),
            currency,
            fxRateToUsd = fxService.GetUsdRate(currency)
        };
    });

    var personas = new object[]
    {
        new { id = "clm-tan", name = "Tan Wei Ming", role = "Claimant", market = "SG", title = "Policyholder (Singapore - Motor & Property)", email = "weiming.tan@example.sg", phone = "+65 9123 4567" },
        new { id = "clm-chloe", name = "Chloe Chan", role = "Claimant", market = "HK", title = "Policyholder (Hong Kong - Commercial Property)", email = "chloe.chan@example.hk", phone = "+852 9388 1120" },
        new { id = "clm-liam", name = "Liam O'Connor", role = "Claimant", market = "AU", title = "Policyholder (Australia - Motor & Property)", email = "liam.oconnor@example.com.au", phone = "+61 412 889 301" },
        new { id = "off-sarah", name = "Sarah Jenkins", role = "ClaimsOfficer", market = "SG, HK", title = "Senior Claims Adjuster (SG & HK)", email = "sarah.jenkins@chubb.com", phone = "+65 6333 8800" },
        new { id = "off-raj", name = "Rajesh Patel", role = "ClaimsOfficer", market = "AU, MY", title = "Claims Specialist (AU & MY)", email = "rajesh.patel@chubb.com", phone = "+61 2 9273 0100" },
        new { id = "off-narong", name = "Narong Srisai", role = "ClaimsOfficer", market = "TH, ID", title = "Regional Claims Officer (TH & ID)", email = "narong.srisai@chubb.com", phone = "+66 2 611 4000" },
        new { id = "mgr-victoria", name = "Victoria Lim", role = "ClaimsManager", market = "APAC (All 6 Markets)", title = "VP, APAC Claims Operations & Exposure", email = "victoria.lim@chubb.com", phone = "+65 6333 8000" }
    };

    return Results.Ok(new
    {
        markets,
        claimTypes = Enum.GetNames<ClaimType>(),
        claimStatuses = Enum.GetNames<ClaimStatus>(),
        personas
    });
});

// 1. Query Claims List
api.MapGet("/claims", async (
    [FromQuery] string? market,
    [FromQuery] string? claimType,
    [FromQuery] string? status,
    [FromQuery] string? claimantId,
    [FromQuery] string? assignedOfficerId,
    [FromQuery] string? search,
    IDispatcher dispatcher,
    CancellationToken ct) =>
{
    var result = await dispatcher.QueryAsync(
        new GetClaimsListQuery(market, claimType, status, claimantId, assignedOfficerId, search), ct);
    return Results.Ok(result);
});

// 2. Query Claim Detail By Id (Automatically masks internal officer notes if caller is a Claimant)
api.MapGet("/claims/{id:guid}", async (
    Guid id,
    HttpContext http,
    IDispatcher dispatcher,
    CancellationToken ct) =>
{
    var (_, _, role) = ResolveActor(http, "clm-tan", "Tan Wei Ming", "Claimant");
    var isStaffRole = !role.Equals("Claimant", StringComparison.OrdinalIgnoreCase);

    var result = await dispatcher.QueryAsync(new GetClaimByIdQuery(id, isStaffRole), ct);
    return Results.Ok(result);
});

// 3. Submit New Claim (FNOL)
api.MapPost("/claims", async (
    [FromBody] SubmitClaimRequest req,
    HttpContext http,
    IDispatcher dispatcher,
    CancellationToken ct) =>
{
    var (actorId, actorName, _) = ResolveActor(http, "clm-tan", "Tan Wei Ming", "Claimant");
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
    return Results.Created($"/api/claims/{created.Id}", created);
});

// 4. Officer Picks Up Claim
api.MapPost("/claims/{id:guid}/pickup", async (
    Guid id,
    [FromBody] PickUpClaimRequest? req,
    HttpContext http,
    IDispatcher dispatcher,
    CancellationToken ct) =>
{
    var (actorId, actorName, _) = ResolveActor(http, "off-sarah", "Sarah Jenkins", "ClaimsOfficer");
    var command = new PickUpClaimCommand(
        id,
        !string.IsNullOrWhiteSpace(req?.OfficerId) ? req.OfficerId : actorId,
        !string.IsNullOrWhiteSpace(req?.OfficerName) ? req.OfficerName : actorName);

    var updated = await dispatcher.SendAsync(command, ct);
    return Results.Ok(updated);
});

// 5. Officer Updates Assessment & Reserve Exposure
api.MapPut("/claims/{id:guid}/assessment", async (
    Guid id,
    [FromBody] UpdateAssessmentRequest req,
    HttpContext http,
    IDispatcher dispatcher,
    CancellationToken ct) =>
{
    var (actorId, actorName, _) = ResolveActor(http, "off-sarah", "Sarah Jenkins", "ClaimsOfficer");
    var command = new UpdateClaimAssessmentCommand(
        id,
        req.AssessedReserve,
        req.InternalNote,
        !string.IsNullOrWhiteSpace(req.OfficerId) ? req.OfficerId : actorId,
        !string.IsNullOrWhiteSpace(req.OfficerName) ? req.OfficerName : actorName);

    var updated = await dispatcher.SendAsync(command, ct);
    return Results.Ok(updated);
});

// 6. Officer Requests Additional Information from Claimant
api.MapPost("/claims/{id:guid}/request-info", async (
    Guid id,
    [FromBody] RequestInfoRequest req,
    HttpContext http,
    IDispatcher dispatcher,
    CancellationToken ct) =>
{
    var (actorId, actorName, _) = ResolveActor(http, "off-sarah", "Sarah Jenkins", "ClaimsOfficer");
    var command = new RequestClaimInfoCommand(
        id,
        req.Question,
        !string.IsNullOrWhiteSpace(req.OfficerId) ? req.OfficerId : actorId,
        !string.IsNullOrWhiteSpace(req.OfficerName) ? req.OfficerName : actorName);

    var updated = await dispatcher.SendAsync(command, ct);
    return Results.Ok(updated);
});

// 7. Claimant Responds to Information Request
api.MapPost("/claims/{id:guid}/respond-info", async (
    Guid id,
    [FromBody] RespondInfoRequest req,
    HttpContext http,
    IDispatcher dispatcher,
    CancellationToken ct) =>
{
    var (actorId, actorName, _) = ResolveActor(http, "clm-tan", "Tan Wei Ming", "Claimant");
    var command = new RespondToClaimInfoCommand(
        id,
        req.Response,
        req.SupportingDocumentRef,
        !string.IsNullOrWhiteSpace(req.ClaimantId) ? req.ClaimantId : actorId,
        !string.IsNullOrWhiteSpace(req.ClaimantName) ? req.ClaimantName : actorName);

    var updated = await dispatcher.SendAsync(command, ct);
    return Results.Ok(updated);
});

// 8. Officer Approves & Settles Claim
api.MapPost("/claims/{id:guid}/settle", async (
    Guid id,
    [FromBody] SettleClaimRequest req,
    HttpContext http,
    IDispatcher dispatcher,
    CancellationToken ct) =>
{
    var (actorId, actorName, _) = ResolveActor(http, "off-sarah", "Sarah Jenkins", "ClaimsOfficer");
    var command = new SettleClaimCommand(
        id,
        req.SettledAmount,
        req.DecisionNotes,
        !string.IsNullOrWhiteSpace(req.OfficerId) ? req.OfficerId : actorId,
        !string.IsNullOrWhiteSpace(req.OfficerName) ? req.OfficerName : actorName);

    var updated = await dispatcher.SendAsync(command, ct);
    return Results.Ok(updated);
});

// 9. Officer Rejects Claim
api.MapPost("/claims/{id:guid}/reject", async (
    Guid id,
    [FromBody] RejectClaimRequest req,
    HttpContext http,
    IDispatcher dispatcher,
    CancellationToken ct) =>
{
    var (actorId, actorName, _) = ResolveActor(http, "off-sarah", "Sarah Jenkins", "ClaimsOfficer");
    var command = new RejectClaimCommand(
        id,
        req.RejectionReason,
        !string.IsNullOrWhiteSpace(req.OfficerId) ? req.OfficerId : actorId,
        !string.IsNullOrWhiteSpace(req.OfficerName) ? req.OfficerName : actorName);

    var updated = await dispatcher.SendAsync(command, ct);
    return Results.Ok(updated);
});

// 10. Real-Time APAC Liability Exposure & Team Workload Analytics
api.MapGet("/analytics/exposure", async (IDispatcher dispatcher, CancellationToken ct) =>
{
    var result = await dispatcher.QueryAsync(new GetExposureAnalyticsQuery(), ct);
    return Results.Ok(result);
});

app.MapHub<ClaimsHub>("/hubs/claims");

app.Run();

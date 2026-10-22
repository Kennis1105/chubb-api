using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Contracts.Responses;
using Chubb.Claims.Application.Features.Claims.Commands;
using Chubb.Claims.Application.Features.Claims.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Chubb.Claims.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        services.AddScoped<IDispatcher, Dispatcher>();

        // Register Command Handlers
        services.AddScoped<ICommandHandler<SubmitClaimCommand, ClaimDetailResponse>, SubmitClaimHandler>();
        services.AddScoped<ICommandHandler<PickUpClaimCommand, ClaimDetailResponse>, PickUpClaimHandler>();
        services.AddScoped<ICommandHandler<UpdateClaimAssessmentCommand, ClaimDetailResponse>, UpdateClaimAssessmentHandler>();
        services.AddScoped<ICommandHandler<RequestClaimInfoCommand, ClaimDetailResponse>, RequestClaimInfoHandler>();
        services.AddScoped<ICommandHandler<RespondToClaimInfoCommand, ClaimDetailResponse>, RespondToClaimInfoHandler>();
        services.AddScoped<ICommandHandler<SettleClaimCommand, ClaimDetailResponse>, SettleClaimHandler>();
        services.AddScoped<ICommandHandler<RejectClaimCommand, ClaimDetailResponse>, RejectClaimHandler>();

        // Register Query Handlers
        services.AddScoped<IQueryHandler<GetClaimsListQuery, IReadOnlyList<ClaimSummaryResponse>>, GetClaimsListHandler>();
        services.AddScoped<IQueryHandler<GetClaimByIdQuery, ClaimDetailResponse>, GetClaimByIdHandler>();
        services.AddScoped<IQueryHandler<GetExposureAnalyticsQuery, ExposureAnalyticsResponse>, GetExposureAnalyticsHandler>();

        return services;
    }
}

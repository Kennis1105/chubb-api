using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.DTOs;
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
        services.AddScoped<ICommandHandler<SubmitClaimCommand, ClaimDetailDto>, SubmitClaimHandler>();
        services.AddScoped<ICommandHandler<PickUpClaimCommand, ClaimDetailDto>, PickUpClaimHandler>();
        services.AddScoped<ICommandHandler<UpdateClaimAssessmentCommand, ClaimDetailDto>, UpdateClaimAssessmentHandler>();
        services.AddScoped<ICommandHandler<RequestClaimInfoCommand, ClaimDetailDto>, RequestClaimInfoHandler>();
        services.AddScoped<ICommandHandler<RespondToClaimInfoCommand, ClaimDetailDto>, RespondToClaimInfoHandler>();
        services.AddScoped<ICommandHandler<SettleClaimCommand, ClaimDetailDto>, SettleClaimHandler>();
        services.AddScoped<ICommandHandler<RejectClaimCommand, ClaimDetailDto>, RejectClaimHandler>();

        // Register Query Handlers
        services.AddScoped<IQueryHandler<GetClaimsListQuery, IReadOnlyList<ClaimSummaryDto>>, GetClaimsListHandler>();
        services.AddScoped<IQueryHandler<GetClaimByIdQuery, ClaimDetailDto>, GetClaimByIdHandler>();
        services.AddScoped<IQueryHandler<GetExposureAnalyticsQuery, ExposureAnalyticsDto>, GetExposureAnalyticsHandler>();

        return services;
    }
}

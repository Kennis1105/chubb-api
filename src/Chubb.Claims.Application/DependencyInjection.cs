using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Contracts.Requests;
using Chubb.Claims.Application.Contracts.Responses;
using Chubb.Claims.Application.Features.Claims.Commands;
using Chubb.Claims.Application.Features.Claims.Queries;
using Chubb.Claims.Application.Features.Metadata.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Chubb.Claims.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        services.AddScoped<IDispatcher, Dispatcher>();

        // Register Command Handlers (Request implements ICommand<TResponse>)
        services.AddScoped<ICommandHandler<SubmitClaimRequest, ClaimDetailResponse>, SubmitClaimHandler>();
        services.AddScoped<ICommandHandler<PickUpClaimRequest, ClaimDetailResponse>, PickUpClaimHandler>();
        services.AddScoped<ICommandHandler<UpdateAssessmentRequest, ClaimDetailResponse>, UpdateClaimAssessmentHandler>();
        services.AddScoped<ICommandHandler<RequestInfoRequest, ClaimDetailResponse>, RequestClaimInfoHandler>();
        services.AddScoped<ICommandHandler<RespondInfoRequest, ClaimDetailResponse>, RespondToClaimInfoHandler>();
        services.AddScoped<ICommandHandler<SettleClaimRequest, ClaimDetailResponse>, SettleClaimHandler>();
        services.AddScoped<ICommandHandler<RejectClaimRequest, ClaimDetailResponse>, RejectClaimHandler>();

        // Register Query Handlers (Request implements IQuery<TResponse>)
        services.AddScoped<IQueryHandler<GetClaimsListRequest, IReadOnlyList<ClaimSummaryResponse>>, GetClaimsListHandler>();
        services.AddScoped<IQueryHandler<GetClaimByIdRequest, ClaimDetailResponse>, GetClaimByIdHandler>();
        services.AddScoped<IQueryHandler<GetExposureAnalyticsRequest, ExposureAnalyticsResponse>, GetExposureAnalyticsHandler>();
        services.AddScoped<IQueryHandler<GetBootstrapMetadataRequest, BootstrapMetadataResponse>, GetBootstrapMetadataHandler>();

        return services;
    }
}

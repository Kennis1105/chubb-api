using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Contracts.Responses;

namespace Chubb.Claims.Application.Contracts.Requests;

public sealed record GetExposureAnalyticsRequest() : IQuery<ExposureAnalyticsResponse>;

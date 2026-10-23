using Chubb.Claims.Application.Common.Interfaces;
using Chubb.Claims.Infrastructure.Messaging;
using Chubb.Claims.Infrastructure.Persistence;
using Chubb.Claims.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chubb.Claims.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ClaimsDb") ?? "Data Source=chubb_apac_claims.db";

        services.AddDbContext<ClaimsDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IClaimRepository, ClaimRepository>();
        services.AddSingleton<IFxRateService, ApacFxRateService>();

        services.AddSingleton<KafkaHybridEventProducer>();
        services.AddSingleton<IKafkaHybridEventProducer>(sp => sp.GetRequiredService<KafkaHybridEventProducer>());
        services.AddSingleton<IEventStreamBroadcaster>(sp => sp.GetRequiredService<KafkaHybridEventProducer>());

        return services;
    }
}

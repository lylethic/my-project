using System;
using Play.Infrastructure.Common.Utilities;

namespace Play.APIs.Configuration;

public static class RedisServiceExtensions
{
    public static IServiceCollection AddRedisConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var envReader = new EnvReader();
        var redisConnectionString = envReader.GetString("REDIS_CONNECTION") ?? "redis:6379";
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "PlayRedisInstance";
        });

        return services;
    }
}


using System;
using Microsoft.Extensions.Caching.Distributed;
using Play.Infrastructure.Common.Caching;
using Play.Infrastructure.Common.Utilities;

namespace Play.APIs.Configuration;

public static class RedisServiceExtensions
{
    public static IServiceCollection AddRedisConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("RedisConnection");
        services.AddStackExchangeRedisCache(options =>
            options.Configuration = redisConnectionString);
        ;
        services.AddOutputCache(options =>
        {
            options.AddBasePolicy(x => x.Expire(TimeSpan.FromSeconds(60)));
            options.AddPolicy("MyCustom", x => x.Expire(TimeSpan.FromSeconds(30)));
        });

        return services;
    }
}


using System;
using System.Text.Json.Serialization;

namespace Play.APIs.Configuration;

public static class SignalRConfig
{
    public static IServiceCollection AddSignalRConfiguration(this IServiceCollection services)
    {
        // Add SignalR services
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true; // Enable detailed errors for debugging
        });

        // Configure JSON serialization options for SignalR
        services.AddSignalR().AddJsonProtocol(options =>
        {
            options.PayloadSerializerOptions.PropertyNamingPolicy = null; // Use default property naming policy
            options.PayloadSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull; // Ignore null values
        });

        return services;
    }
}

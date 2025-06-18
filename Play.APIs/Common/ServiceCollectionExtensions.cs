// Create this file: Play.APIs/Extensions/ServiceCollectionExtensions.cs
using Microsoft.Extensions.DependencyInjection;
using Play.Infrastructure.Common.Contracts;
using Play.Infrastructure.Repository;
using Play.Infrastructure.Services;

namespace Play.APIs.Common;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var infrastructureAssembly = typeof(RoleService).Assembly;

        // Register Scoped services
        services.Scan(scan => scan
            .FromAssemblies(infrastructureAssembly)
            .AddClasses(classes => classes.AssignableTo<IScoped>())
            .AsSelf()
            .WithScopedLifetime());

        // Register Singleton services
        services.Scan(scan => scan
            .FromAssemblies(infrastructureAssembly)
            .AddClasses(classes => classes.AssignableTo<ISingleton>())
            .AsSelf()
            .WithSingletonLifetime());

        // Register Transient services
        services.Scan(scan => scan
            .FromAssemblies(infrastructureAssembly)
            .AddClasses(classes => classes.AssignableTo<ITransient>())
            .AsSelf()
            .WithTransientLifetime());

        return services;
    }
}

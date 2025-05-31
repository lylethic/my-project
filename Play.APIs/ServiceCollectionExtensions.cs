// Create this file: Play.APIs/Extensions/ServiceCollectionExtensions.cs
using Microsoft.Extensions.DependencyInjection;
using Play.Infrastructure.Common.Contracts;
using Play.Infrastructure.Repository;
using Play.Infrastructure.Services;

namespace Play.APIs.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register all services that implement IScoped from Infrastructure assemblies
            services.Scan(scan => scan
                .FromAssemblies(
                    typeof(RoleService).Assembly,        // Play.Infrastructure assembly
                    typeof(RoleRepo).Assembly            // In case repos are in different assembly
                )
                .AddClasses(classes => classes.AssignableTo<IScoped>())
                .AsSelf()
                .WithScopedLifetime());

            // Register all services that implement ISingleton
            services.Scan(scan => scan
                .FromAssemblies(
                    typeof(RoleService).Assembly,
                    typeof(RoleRepo).Assembly
                )
                .AddClasses(classes => classes.AssignableTo<ISingleton>())
                .AsSelf()
                .WithSingletonLifetime());

            // Register all services that implement ITransient
            services.Scan(scan => scan
                .FromAssemblies(
                    typeof(RoleService).Assembly,
                    typeof(RoleRepo).Assembly
                )
                .AddClasses(classes => classes.AssignableTo<ITransient>())
                .AsSelf()
                .WithTransientLifetime());

            return services;
        }
    }
}
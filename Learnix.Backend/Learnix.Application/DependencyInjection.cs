using System.Reflection;
using FluentResults;
using FluentValidation;
using Learnix.Application.Categories.Services;
using Learnix.Application.Common.Behaviors;
using Learnix.Application.Common.Caching;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Learnix.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(DomainExceptionBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<CategoryCoursesCountUpdater>();
        services.AddCachingBehaviors(assembly);

        return services;
    }

    // Manually registers a closed IPipelineBehavior<,> for every request type
    // implementing ICacheable<TValue>, since the DI container maps generic
    // arguments positionally and cannot infer TValue from a nested Result<TValue>.
    private static void AddCachingBehaviors(this IServiceCollection services, Assembly assembly)
    {
        var cacheableRequests = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICacheable<>))
                .Select(i => new
                {
                    RequestType = t,
                    ValueType = i.GetGenericArguments()[0]
                }));

        foreach (var entry in cacheableRequests)
        {
            var resultType = typeof(Result<>).MakeGenericType(entry.ValueType);
            var serviceType = typeof(IPipelineBehavior<,>).MakeGenericType(entry.RequestType, resultType);
            var implementationType = typeof(CachingBehavior<,>).MakeGenericType(entry.RequestType, entry.ValueType);

            services.AddScoped(serviceType, implementationType);
        }
    }
}

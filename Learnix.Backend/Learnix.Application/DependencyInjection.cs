using System.Reflection;
using FluentValidation;
using Learnix.Application.Categories.Services;
using Learnix.Application.Common.Behaviors;
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
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<CategoryCoursesCountUpdater>();

        return services;
    }
}
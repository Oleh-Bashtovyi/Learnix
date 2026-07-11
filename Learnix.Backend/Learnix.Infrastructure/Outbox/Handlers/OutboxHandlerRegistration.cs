using Microsoft.Extensions.DependencyInjection;

namespace Learnix.Infrastructure.Outbox.Handlers;

internal static class OutboxHandlerRegistration
{
    /// <summary>
    /// Registers every <see cref="IOutboxMessageHandler"/> in this assembly, the way MediatR and
    /// FluentValidation are wired: a new message type is a new class, and nothing else has to be told about
    /// it. <see cref="OutboxMessageDispatcher"/> refuses to start if two of them claim the same type.
    /// </summary>
    public static IServiceCollection AddOutboxMessageHandlers(this IServiceCollection services)
    {
        var handlerTypes = typeof(OutboxHandlerRegistration).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && typeof(IOutboxMessageHandler).IsAssignableFrom(t));

        foreach (var handlerType in handlerTypes)
            services.AddScoped(typeof(IOutboxMessageHandler), handlerType);

        services.AddScoped<IOutboxMessageDispatcher, OutboxMessageDispatcher>();

        return services;
    }
}

using Learnix.API.Services;
using Learnix.API.Services.Notifications;
using Learnix.Application.Achievements.Abstractions;
using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Messaging.Abstractions;
using Learnix.Application.Notifications.Abstractions;


namespace Learnix.API.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSignalR();

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IChatNotifier, SignalRChatNotifier>();
        services.AddScoped<IAchievementNotifier, SignalRAchievementNotifier>();
        services.AddScoped<ICertificateNotifier, SignalRCertificateNotifier>();
        services.AddScoped<INotificationSender, SignalRNotificationSender>();

        return services;
    }
}

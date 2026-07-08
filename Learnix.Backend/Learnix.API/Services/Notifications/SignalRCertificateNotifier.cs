using Learnix.API.Hubs;
using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Common.Abstractions.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Learnix.API.Services.Notifications;

internal sealed class SignalRCertificateNotifier(
    IHubContext<NotificationsHub, INotificationsHubClient> hubContext)
    : ICertificateNotifier
{
    public Task NotifyCertificateReadyAsync(Guid userId, Guid certificateId, string courseTitle, CancellationToken ct)
        => hubContext.Clients
            .Group(NotificationsHub.UserGroup(userId.ToString()))
            .CertificateReady(new CertificateReadyNotification(certificateId, courseTitle));
}

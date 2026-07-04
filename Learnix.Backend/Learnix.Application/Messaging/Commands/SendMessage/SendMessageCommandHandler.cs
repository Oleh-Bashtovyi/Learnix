using FluentResults;
using Learnix.Application.Common.Abstractions.Hubs;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Messaging.Abstractions;
using Learnix.Application.Messaging.Constants;
using Learnix.Application.Messaging.Specifications;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Users.Specifications;
using MediatR;

namespace Learnix.Application.Messaging.Commands.SendMessage;

public sealed class SendMessageCommandHandler(
    ICurrentUserService currentUser,
    IConversationRepository conversationRepository,
    IMessageRepository messageRepository,
    IUserRepository userRepository,
    IChatNotifier chatNotifier,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SendMessageCommand, Result<SendMessageResponse>>
{
    public async Task<Result<SendMessageResponse>> Handle(
        SendMessageCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var senderId = currentUser.UserId.Value;

        var conversation = await conversationRepository.FirstOrDefaultAsync(
            new ConversationByIdSpecification(request.ConversationId, forUpdate: true),
            cancellationToken);

        if (conversation is null)
            return Result.Fail(new NotFoundError(MessagingMessages.ConversationNotFound));

        if (conversation.StudentId != senderId && conversation.InstructorId != senderId)
            return Result.Fail(new ForbiddenError(MessagingMessages.NotAParticipant));

        var sender = await userRepository.FirstOrDefaultAsync(
            new UserByIdSpecification(senderId), cancellationToken);

        if (sender is null)
            return Result.Fail(new NotFoundError(MessagingMessages.SenderNotFound));

        var message = conversation.AddMessage(senderId, request.Content);

        await messageRepository.AddAsync(message, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var recipientId = senderId == conversation.StudentId
            ? conversation.InstructorId
            : conversation.StudentId;

        var senderName = $"{sender.FirstName} {sender.LastName}";

        var notification = new NewMessageNotification(
            conversation.Id,
            message.Id,
            senderId,
            senderName,
            sender.AvatarBlobPath,
            message.Content,
            message.CreatedAt);

        await chatNotifier.NotifyNewMessageAsync(recipientId, notification, cancellationToken);

        var recipientUnreadCount = senderId == conversation.StudentId
            ? conversation.InstructorUnreadCount
            : conversation.StudentUnreadCount;

        await chatNotifier.NotifyUnreadCountChangedAsync(recipientId, recipientUnreadCount, cancellationToken);

        return Result.Ok(new SendMessageResponse(
            message.Id,
            conversation.Id,
            senderId,
            senderName,
            sender.AvatarBlobPath,
            message.Content,
            message.CreatedAt));
    }
}

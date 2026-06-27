using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Messaging.Abstractions;
using Learnix.Application.Messaging.Constants;
using Learnix.Application.Messaging.Specifications;
using MediatR;

namespace Learnix.Application.Messaging.Commands.MarkConversationRead;

public sealed class MarkConversationReadCommandHandler(
    ICurrentUserService currentUser,
    IConversationRepository conversationRepository,
    IChatNotifier chatNotifier,
    IUnitOfWork unitOfWork)
    : IRequestHandler<MarkConversationReadCommand, Result>
{
    public async Task<Result> Handle(
        MarkConversationReadCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var userId = currentUser.UserId.Value;

        var conversation = await conversationRepository.FirstOrDefaultAsync(
            new ConversationByIdSpecification(request.ConversationId, forUpdate: true),
            cancellationToken);

        if (conversation is null)
            return Result.Fail(new NotFoundError(MessagingMessages.ConversationNotFound));

        if (conversation.StudentId != userId && conversation.InstructorId != userId)
            return Result.Fail(new ForbiddenError(MessagingMessages.NotAParticipant));

        var isInstructor = conversation.InstructorId == userId;

        if (isInstructor)
            conversation.MarkReadByInstructor();
        else
            conversation.MarkReadByStudent();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var newTotal = await conversationRepository.GetTotalUnreadAsync(userId, isInstructor, cancellationToken);
        await chatNotifier.NotifyUnreadCountChangedAsync(userId, newTotal, cancellationToken);

        return Result.Ok();
    }
}

using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Messaging.Abstractions;
using Learnix.Application.Messaging.Constants;
using Learnix.Application.Messaging.Specifications;
using MediatR;

namespace Learnix.Application.Messaging.Commands.BlockConversation;

public sealed class BlockConversationCommandHandler(
    ICurrentUserService currentUser,
    IConversationRepository conversationRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<BlockConversationCommand, Result>
{
    public async Task<Result> Handle(BlockConversationCommand request, CancellationToken cancellationToken)
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

        if (conversation.IsBlocked)
            return Result.Fail(new ConflictError(MessagingMessages.AlreadyBlocked));

        conversation.Block(userId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}

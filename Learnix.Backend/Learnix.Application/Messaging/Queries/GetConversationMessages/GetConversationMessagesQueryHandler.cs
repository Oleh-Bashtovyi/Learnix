using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Common.Pagination;
using Learnix.Application.Messaging.Abstractions;
using Learnix.Application.Messaging.Specifications;
using MediatR;

namespace Learnix.Application.Messaging.Queries.GetConversationMessages;

public sealed class GetConversationMessagesQueryHandler(
    ICurrentUserService currentUser,
    IConversationRepository conversationRepository,
    IMessageRepository messageRepository)
    : IRequestHandler<GetConversationMessagesQuery, Result<PaginatedResult<MessageDto>>>
{
    public async Task<Result<PaginatedResult<MessageDto>>> Handle(
        GetConversationMessagesQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var userId = currentUser.UserId.Value;

        var conversation = await conversationRepository.FirstOrDefaultAsync(
            new ConversationByIdSpecification(request.ConversationId), cancellationToken);

        if (conversation is null)
            return Result.Fail(new NotFoundError("Conversation not found."));

        if (conversation.StudentId != userId && conversation.InstructorId != userId)
            return Result.Fail(new ForbiddenError("You are not a participant of this conversation."));

        var pagination = PaginationRequest.FromOffset(request.Skip, request.Take);

        var totalCount = await messageRepository.CountAsync(
            new MessagesCountByConversationSpecification(request.ConversationId), cancellationToken);

        if (totalCount == 0)
            return Result.Ok(PaginatedResult<MessageDto>.Empty(pagination.PageIndex, pagination.PageSize));

        var messages = await messageRepository.ListAsync(
            new MessagesByConversationSpecification(request.ConversationId, pagination.Skip, pagination.Take),
            cancellationToken);

        var dtos = messages.Select(m => new MessageDto(
            m.Id,
            m.SenderId,
            $"{m.Sender!.FirstName} {m.Sender.LastName}",
            m.Sender.AvatarBlobPath,
            m.Content,
            m.CreatedAt,
            m.SenderId == userId));

        return Result.Ok(PaginatedResult<MessageDto>.Create(
            dtos, pagination.PageIndex, pagination.PageSize, totalCount));
    }
}

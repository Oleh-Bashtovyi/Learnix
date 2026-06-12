using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Messaging.Abstractions;
using Learnix.Application.Messaging.Specifications;
using Learnix.Domain.Constants;
using MediatR;

namespace Learnix.Application.Messaging.Queries.GetMyConversations;

public sealed class GetMyConversationsQueryHandler(
    ICurrentUserService currentUser,
    IConversationRepository conversationRepository)
    : IRequestHandler<GetMyConversationsQuery, Result<List<ConversationSummaryDto>>>
{
    public async Task<Result<List<ConversationSummaryDto>>> Handle(
        GetMyConversationsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var userId = currentUser.UserId.Value;
        var isInstructor = currentUser.IsInRole(Roles.Instructor);

        var conversations = isInstructor
            ? await conversationRepository.ListAsync(
                new ConversationsByInstructorSpecification(userId), cancellationToken)
            : await conversationRepository.ListAsync(
                new ConversationsByStudentSpecification(userId), cancellationToken);

        var dtos = conversations.Select(c =>
        {
            var otherUser = isInstructor ? c.Student! : c.Instructor!;
            var unreadCount = isInstructor ? c.InstructorUnreadCount : c.StudentUnreadCount;

            return new ConversationSummaryDto(
                c.Id,
                c.CourseId,
                c.Course!.Title,
                otherUser.Id,
                $"{otherUser.FirstName} {otherUser.LastName}",
                otherUser.AvatarBlobPath,
                c.LastMessagePreview,
                c.LastMessageAt,
                unreadCount);
        }).ToList();

        return Result.Ok(dtos);
    }
}

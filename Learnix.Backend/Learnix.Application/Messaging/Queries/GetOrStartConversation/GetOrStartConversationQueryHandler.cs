using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Specifications;
using Learnix.Application.Messaging.Abstractions;
using Learnix.Application.Messaging.Specifications;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Users.Specifications;
using Learnix.Domain.Entities;
using MediatR;

namespace Learnix.Application.Messaging.Queries.GetOrStartConversation;

public sealed class GetOrStartConversationQueryHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    IEnrollmentRepository enrollmentRepository,
    IConversationRepository conversationRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GetOrStartConversationQuery, Result<ConversationDto>>
{
    public async Task<Result<ConversationDto>> Handle(
        GetOrStartConversationQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var studentId = currentUser.UserId.Value;

        var enrollment = await enrollmentRepository.FirstOrDefaultAsync(
            new ActiveEnrollmentByStudentAndCourseSpecification(studentId, request.CourseId),
            cancellationToken);

        if (enrollment is null)
            return Result.Fail(new ForbiddenError("You must be enrolled in this course to send messages."));

        var course = await courseRepository.FirstOrDefaultAsync(
            new CourseByIdSpecification(request.CourseId), cancellationToken);

        if (course is null)
            return Result.Fail(new NotFoundError(CommonMessages.CourseNotFound(request.CourseId)));

        var instructor = await userRepository.FirstOrDefaultAsync(
            new UserByIdSpecification(course.InstructorId), cancellationToken);

        var instructorName = instructor?.UserName ?? string.Empty;
        var instructorAvatarPath = instructor?.AvatarBlobPath;

        var existing = await conversationRepository.FirstOrDefaultAsync(
            new ConversationByCourseAndStudentSpecification(request.CourseId, studentId),
            cancellationToken);

        if (existing is not null)
        {
            return Result.Ok(new ConversationDto(
                existing.Id,
                existing.CourseId,
                course.Title,
                course.InstructorId,
                instructorName,
                instructorAvatarPath,
                existing.StudentUnreadCount));
        }

        var conversation = CourseConversation.Create(request.CourseId, studentId, course.InstructorId);
        await conversationRepository.AddAsync(conversation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(new ConversationDto(
            conversation.Id,
            conversation.CourseId,
            course.Title,
            course.InstructorId,
            instructorName,
            instructorAvatarPath,
            0));
    }
}

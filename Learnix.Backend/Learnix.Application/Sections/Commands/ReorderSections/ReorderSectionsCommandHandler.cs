using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Commands;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Sections.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.Sections.Commands.ReorderSections;

internal sealed class ReorderSectionsCommandHandler(
    ICourseRepository courseRepository,
    ISectionRepository sectionRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : CourseCommandHandler<ReorderSectionsCommand, Result>(courseRepository, currentUser)
{
    protected override async Task<Result> HandleAsync(
        ReorderSectionsCommand request, Course course, CancellationToken ct)
    {
        var pairs = request.Items.Select(i => (i.Id, i.Order)).ToList();

        course.ReorderSections(pairs); // domain validation

        await unitOfWork.ExecuteInTransactionAsync(
            () => sectionRepository.BulkSetDisplayOrderAsync(pairs, ct), ct);

        return Result.Ok();
    }
}

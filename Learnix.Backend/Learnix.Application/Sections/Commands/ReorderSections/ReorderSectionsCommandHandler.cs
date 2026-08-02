using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Commands;
using Learnix.Application.Courses.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.Sections.Commands.ReorderSections;

internal sealed class ReorderSectionsCommandHandler(
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : CourseCommandHandler<ReorderSectionsCommand, Result>(courseRepository, currentUser)
{
    protected override async Task<Result> HandleAsync(
        ReorderSectionsCommand request, Course course, CancellationToken cancellationToken)
    {
        var pairs = request.Items.Select(i => (i.Id, i.Order)).ToList();

        course.ReorderSections(pairs);

        // A permutation collides on the unique (CourseId, DisplayOrder) — but that constraint is deferred
        // to COMMIT (see DeferrableConstraint), so a plain SaveChanges applies the whole set at once.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}

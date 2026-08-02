using FluentValidation;

namespace Learnix.Application.InstructorAnalytics.Queries.GetInstructorLessonDropOff;

public sealed class GetInstructorLessonDropOffQueryValidator
    : AbstractValidator<GetInstructorLessonDropOffQuery>
{
    public GetInstructorLessonDropOffQueryValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
    }
}

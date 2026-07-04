namespace Learnix.Application.Common.Commands;

public interface ICommandWithCourseId
{
    Guid CourseId { get; }
}

public interface ICommandWithCourseAndSectionId : ICommandWithCourseId
{
    Guid SectionId { get; }
}

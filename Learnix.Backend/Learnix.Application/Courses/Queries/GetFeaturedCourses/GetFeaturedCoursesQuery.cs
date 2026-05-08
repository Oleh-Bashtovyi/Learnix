using FluentResults;
using MediatR;

namespace Learnix.Application.Courses.Queries.GetFeaturedCourses;

public sealed record GetFeaturedCoursesQuery() : IRequest<Result<IReadOnlyList<FeaturedCourseDto>>>;

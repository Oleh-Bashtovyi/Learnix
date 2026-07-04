using FluentResults;
using MediatR;

namespace Learnix.Application.InstructorApplications.Queries.GetMyApplication;

public record GetMyApplicationQuery : IRequest<Result<MyApplicationResponse?>>;

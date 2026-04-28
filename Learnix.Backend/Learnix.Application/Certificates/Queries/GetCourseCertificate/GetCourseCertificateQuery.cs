using FluentResults;
using MediatR;

namespace Learnix.Application.Certificates.Queries.GetCourseCertificate;

public sealed record GetCourseCertificateQuery(Guid CourseId) : IRequest<Result<CourseCertificateResponse>>;

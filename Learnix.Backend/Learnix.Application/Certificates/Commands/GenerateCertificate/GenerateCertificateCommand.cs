using FluentResults;
using MediatR;

namespace Learnix.Application.Certificates.Commands.GenerateCertificate;

public sealed record GenerateCertificateCommand(Guid CourseId) : IRequest<Result<string>>;

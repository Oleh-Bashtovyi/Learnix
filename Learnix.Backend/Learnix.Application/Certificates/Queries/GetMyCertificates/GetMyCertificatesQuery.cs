using FluentResults;
using MediatR;

namespace Learnix.Application.Certificates.Queries.GetMyCertificates;

public sealed record GetMyCertificatesQuery : IRequest<Result<IReadOnlyList<MyCertificateDto>>>;

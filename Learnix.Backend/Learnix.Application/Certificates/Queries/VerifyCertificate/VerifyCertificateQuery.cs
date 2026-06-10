using FluentResults;
using MediatR;

namespace Learnix.Application.Certificates.Queries.VerifyCertificate;

public sealed record VerifyCertificateQuery(string Code) : IRequest<Result<VerifyCertificateResponse>>;

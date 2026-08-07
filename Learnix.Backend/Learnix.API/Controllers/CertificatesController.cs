using Asp.Versioning;
using Learnix.API.Extensions;
using Learnix.Application.Certificates.Commands.GenerateCertificate;
using Learnix.Application.Certificates.Queries.GetCourseCertificate;
using Learnix.Application.Certificates.Queries.GetMyCertificates;
using Learnix.Application.Certificates.Queries.VerifyCertificate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/certificates")]
[Authorize]
public sealed class CertificatesController(ISender sender) : ControllerBase
{
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyCertificatesQuery(), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpGet("courses/{courseId:guid}")]
    public async Task<IActionResult> GetCourseCertificate(Guid courseId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCourseCertificateQuery(courseId), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpPost("courses/{courseId:guid}/generate")]
    public async Task<IActionResult> GenerateCourseCertificate(Guid courseId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GenerateCertificateCommand(courseId), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(new { url = value }));
    }

    [HttpGet("verify/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> Verify(string code, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new VerifyCertificateQuery(code), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }
}

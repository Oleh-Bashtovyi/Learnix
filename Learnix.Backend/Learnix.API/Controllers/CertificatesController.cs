using Learnix.API.Extensions;
using Learnix.Application.Certificates.Queries.GetCourseCertificate;
using Learnix.Application.Certificates.Queries.GetMyCertificates;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[Route("api/certificates")]
[Authorize]
public sealed class CertificatesController(ISender sender) : ControllerBase
{
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyCertificatesQuery(), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpGet("courses/{courseId:guid}")]
    public async Task<IActionResult> GetCourseCertificate(Guid courseId, CancellationToken ct)
    {
        var result = await sender.Send(new GetCourseCertificateQuery(courseId), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }
}

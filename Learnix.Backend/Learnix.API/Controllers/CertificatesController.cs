using Learnix.API.Extensions;
using Learnix.Application.Certificates.Queries.GetCourseCertificate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[Route("api/certificates")]
[Authorize]
public sealed class CertificatesController(ISender sender) : ControllerBase
{
    [HttpGet("courses/{courseId:guid}")]
    public async Task<IActionResult> GetCourseCertificate(Guid courseId, CancellationToken ct)
    {
        var result = await sender.Send(new GetCourseCertificateQuery(courseId), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }
}

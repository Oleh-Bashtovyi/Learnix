using Asp.Versioning;
using Learnix.API.Extensions;
using Learnix.API.RateLimiting;
using Learnix.Application.Uploads.Commands.RequestUploadUrl;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Learnix.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/uploads")]
[Authorize]
public sealed class UploadsController(ISender mediator) : ControllerBase
{
    [HttpPost("request-url")]
    [EnableRateLimiting(RateLimitPolicies.Uploads)]
    public async Task<IActionResult> RequestUploadUrl(
        [FromBody] RequestUploadUrlCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);

        return result.ToActionResult();
    }
}

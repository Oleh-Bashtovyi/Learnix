using Asp.Versioning;
using Learnix.API.Extensions;
using Learnix.Application.Config.Queries.GetPublicConfig;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class ConfigController(ISender sender) : ControllerBase
{
    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicConfig(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPublicConfigQuery(), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }
}

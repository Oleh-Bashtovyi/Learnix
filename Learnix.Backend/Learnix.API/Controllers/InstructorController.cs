using Learnix.API.Extensions;
using Learnix.Application.Payments.Queries.GetInstructorEarnings;
using Learnix.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[Route("api/instructor")]
[Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
public sealed class InstructorController(ISender sender) : ControllerBase
{
    [HttpGet("earnings")]
    public async Task<IActionResult> GetEarnings(CancellationToken ct)
    {
        var result = await sender.Send(new GetInstructorEarningsQuery(), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }
}

using Learnix.API.Extensions;
using Learnix.Application.Enrollments.Commands.EnrollInCourse;
using Learnix.Application.Enrollments.Queries.GetMyEnrollments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class EnrollmentsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "EmailConfirmed")]
    public async Task<IActionResult> Enroll(
        [FromBody] EnrollInCourseCommand command,
        CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetMyEnrollmentsQuery(skip, take), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }
}

using Asp.Versioning;
using Learnix.API.Constants;
using Learnix.API.Extensions;
using Learnix.Application.Enrollments.Commands.EnrollInCourse;
using Learnix.Application.Enrollments.Queries.GetContinueLearning;
using Learnix.Application.Enrollments.Queries.GetMyEnrollments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class EnrollmentsController(ISender sender) : ControllerBase
{
    /// <summary>Enrolls the signed-in student in a course (free — paid enrollment happens via <c>PaymentsController</c>).</summary>
    [HttpPost]
    [Authorize(Policy = AuthPolicies.EmailConfirmed)]
    public async Task<IActionResult> Enroll(
        [FromBody] EnrollInCourseCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    /// <summary>Lists the signed-in student's enrollments.</summary>
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetMyEnrollmentsQuery(skip, take), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    /// <summary>Returns 204 when the student has no course in progress.</summary>
    [HttpGet("continue")]
    public async Task<IActionResult> GetContinueLearning(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetContinueLearningQuery(), cancellationToken);
        return result.ToActionResult(onSuccess: value => value is null ? NoContent() : Ok(value));
    }
}

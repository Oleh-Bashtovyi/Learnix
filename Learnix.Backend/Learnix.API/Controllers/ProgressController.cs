using Learnix.API.Extensions;
using Learnix.Application.LessonProgress.Commands.MarkLessonComplete;
using Learnix.Application.LessonProgress.Queries.GetCourseProgress;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[Route("api/progress")]
[Authorize]
public sealed class ProgressController(ISender sender) : ControllerBase
{
    [HttpPost("courses/{courseId:guid}/lessons/{lessonId:guid}/complete")]
    public async Task<IActionResult> MarkLessonComplete(
        Guid courseId,
        Guid lessonId,
        CancellationToken ct)
    {
        var result = await sender.Send(new MarkLessonCompleteCommand(courseId, lessonId), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpGet("courses/{courseId:guid}")]
    public async Task<IActionResult> GetCourseProgress(Guid courseId, CancellationToken ct)
    {
        var result = await sender.Send(new GetCourseProgressQuery(courseId), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }
}

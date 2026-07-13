using System.Text.Json.Serialization;
using Learnix.API.Extensions;
using Learnix.Application.Common.Models;
using Learnix.Application.Lessons.Commands.CreatePostLesson;
using Learnix.Application.Lessons.Commands.CreateTestLesson;
using Learnix.Application.Lessons.Commands.CreateVideoLesson;
using Learnix.Application.Lessons.Commands.DeleteLesson;
using Learnix.Application.Lessons.Commands.ReorderLessons;
using Learnix.Application.Lessons.Commands.ToggleLessonVisibility;
using Learnix.Application.Lessons.Commands.UpdatePostLesson;
using Learnix.Application.Lessons.Commands.UpdateTestLesson;
using Learnix.Application.Lessons.Commands.UpdateVideoLesson;
using Learnix.Application.Lessons.Queries.GetLessonContent;
using Learnix.Domain.Constants;
using Learnix.Domain.Enums;
using Learnix.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[Authorize]
[Route("api/courses/{courseId:guid}")]
public sealed class LessonsController(ISender sender) : ControllerBase
{
    public sealed record CreateVideoLessonRequest(
        string Title, string VideoUrl, string? Description, int? DurationSeconds);

    public sealed record CreatePostLessonRequest(string Title, string Content);

    public sealed record UpdateVideoLessonRequest(
        string Title, string VideoUrl, string? Description, int? DurationSeconds);

    public sealed record UpdatePostLessonRequest(string Title, string Content);

    public sealed record ReorderLessonsRequest(IReadOnlyList<ReorderItem> Items);

    // JsonRequired on the value-type members: without it an omitted field silently binds to
    // default(T) — a missing "isVisible" would hide the lesson, a missing threshold would make
    // every test pass at 0%. The client has to say what it means.
    public sealed record ToggleLessonVisibilityRequest([property: JsonRequired] bool IsVisible);

    public sealed record CreateTestLessonRequest(
        string Title,
        string? Description,
        int? AttemptLimit,
        int? CooldownMinutes,
        [property: JsonRequired] int PassingThreshold,
        [property: JsonRequired] TestReviewMode ReviewMode,
        IReadOnlyList<QuestionBlueprint> Questions);

    public sealed record UpdateTestLessonRequest(
        string Title,
        string? Description,
        int? AttemptLimit,
        int? CooldownMinutes,
        [property: JsonRequired] int PassingThreshold,
        [property: JsonRequired] TestReviewMode ReviewMode,
        IReadOnlyList<QuestionBlueprint> Questions);

    // Get lesson content (student)

    [HttpGet("lessons/{lessonId:guid}")]
    public async Task<IActionResult> GetContent(Guid courseId, Guid lessonId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetLessonContentQuery(courseId, lessonId), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    // Create

    [HttpPost("sections/{sectionId:guid}/lessons/video")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> CreateVideo(
        Guid courseId,
        Guid sectionId,
        [FromBody] CreateVideoLessonRequest body,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateVideoLessonCommand(
                courseId, sectionId, body.Title, body.VideoUrl, body.Description, body.DurationSeconds),
            cancellationToken);
        return result.ToActionResult(id => CreatedAtAction(nameof(CreateVideo),
            new { courseId, sectionId }, new { id }));
    }

    [HttpPost("sections/{sectionId:guid}/lessons/test")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> CreateTest(
        Guid courseId,
        Guid sectionId,
        [FromBody] CreateTestLessonRequest body,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateTestLessonCommand(
                courseId, sectionId, body.Title, body.Description,
                body.AttemptLimit, body.CooldownMinutes, body.PassingThreshold,
                body.ReviewMode, body.Questions),
            cancellationToken);
        return result.ToActionResult(id => CreatedAtAction(nameof(CreateTest),
            new { courseId, sectionId }, new { id }));
    }

    [HttpPost("sections/{sectionId:guid}/lessons/post")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> CreatePost(
        Guid courseId,
        Guid sectionId,
        [FromBody] CreatePostLessonRequest body,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreatePostLessonCommand(courseId, sectionId, body.Title, body.Content), cancellationToken);
        return result.ToActionResult(id => CreatedAtAction(nameof(CreatePost),
            new { courseId, sectionId }, new { id }));
    }

    // Update

    [HttpPatch("lessons/{lessonId:guid}/video")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> UpdateVideo(
        Guid courseId,
        Guid lessonId,
        [FromBody] UpdateVideoLessonRequest body,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateVideoLessonCommand(
                courseId, lessonId, body.Title, body.VideoUrl, body.Description, body.DurationSeconds),
            cancellationToken);
        return result.ToActionResult();
    }

    [HttpPatch("lessons/{lessonId:guid}/test")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> UpdateTest(
        Guid courseId,
        Guid lessonId,
        [FromBody] UpdateTestLessonRequest body,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateTestLessonCommand(
                courseId, lessonId, body.Title, body.Description,
                body.AttemptLimit, body.CooldownMinutes, body.PassingThreshold,
                body.ReviewMode, body.Questions),
            cancellationToken);
        return result.ToActionResult();
    }

    [HttpPatch("lessons/{lessonId:guid}/post")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> UpdatePost(
        Guid courseId,
        Guid lessonId,
        [FromBody] UpdatePostLessonRequest body,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdatePostLessonCommand(courseId, lessonId, body.Title, body.Content), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPatch("lessons/{lessonId:guid}/toggle-visibility")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> ToggleVisibility(
        Guid courseId,
        Guid lessonId,
        [FromBody] ToggleLessonVisibilityRequest body,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ToggleLessonVisibilityCommand(courseId, lessonId, body.IsVisible), cancellationToken);
        return result.ToActionResult();
    }

    // Delete / Reorder
    [HttpDelete("lessons/{lessonId:guid}")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> Delete(Guid courseId, Guid lessonId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteLessonCommand(courseId, lessonId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("sections/{sectionId:guid}/lessons/reorder")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> Reorder(
        Guid courseId,
        Guid sectionId,
        [FromBody] ReorderLessonsRequest body,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ReorderLessonsCommand(courseId, sectionId, body.Items), cancellationToken);
        return result.ToActionResult();
    }
}

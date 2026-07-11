using System.Text.Json.Serialization;
using Learnix.API.Extensions;
using Learnix.Application.Courses.Commands.ArchiveCourse;
using Learnix.Application.Courses.Commands.CreateCourse;
using Learnix.Application.Courses.Commands.DeleteCourse;
using Learnix.Application.Courses.Commands.PublishCourse;
using Learnix.Application.Courses.Commands.UnarchiveCourse;
using Learnix.Application.Courses.Commands.UnpublishCourse;
using Learnix.Application.Courses.Commands.UpdateCourseDetails;
using Learnix.Application.Courses.Queries.GetAdminCourses;
using Learnix.Application.Courses.Queries.GetCourseById;
using Learnix.Application.Courses.Queries.GetCourseForEditById;
using Learnix.Application.Courses.Queries.GetFeaturedCourses;
using Learnix.Application.Courses.Queries.GetInstructorCourses;
using Learnix.Application.Courses.Queries.GetPublicCourses;
using Learnix.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class CoursesController(ISender sender) : ControllerBase
{
    // S107: these are the catalog's query-string parameters, not a call signature we chose. Binding them
    // through a record would drop the C# default values (the MVC binder ignores them, so take would come
    // in as 0 instead of 20) and Swagger would still list them one by one.
#pragma warning disable S107
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicList(
        [FromQuery] string? search,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? instructorId = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool? isFree = null,
        [FromQuery] decimal? minRating = null,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetPublicCoursesQuery(search, skip, take, categoryId, instructorId, sortBy, isFree, minRating), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }
#pragma warning restore S107

    [HttpGet("featured")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFeatured(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetFeaturedCoursesQuery(), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCourseByIdQuery(id), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpGet("mine")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> GetMine(
        [FromQuery] string? search,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetInstructorCoursesQuery(search, skip, take, categoryId), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpGet("admin")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetAllForAdmin(
        [FromQuery] string? search,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetAdminCoursesQuery(search, skip, take, categoryId), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpGet("{id:guid}/edit")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> GetForEdit(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCourseForEditByIdQuery(id), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> Create(
        [FromBody] CreateCourseCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult(onSuccess: value =>
            CreatedAtAction(nameof(GetById), new { id = value.CourseId }, value));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCourseRequest body,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCourseDetailsCommand(
            id,
            body.CategoryId,
            body.Title,
            body.Description,
            body.Price,
            body.CoverImageUrl,
            body.Tags);

        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PublishCourseCommand(id), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/unpublish")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> Unpublish(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UnpublishCourseCommand(id), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ArchiveCourseCommand(id), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/unarchive")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> Unarchive(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UnarchiveCourseCommand(id), cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteCourseCommand(id), cancellationToken);
        return result.ToActionResult();
    }
}

public sealed record UpdateCourseRequest(
    [property: JsonRequired] Guid CategoryId,
    string Title,
    string Description,
    [property: JsonRequired] decimal Price,
    string? CoverImageUrl,
    IEnumerable<string> Tags);

using System.Text.Json.Serialization;
using Asp.Versioning;
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
using Learnix.Application.Courses.Queries.GetPopularTags;
using Learnix.Application.Courses.Queries.GetPublicCourses;
using Learnix.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class CoursesController(ISender sender) : ControllerBase
{
    // S107: these are the catalog's query-string parameters, not a call signature we chose. Binding them
    // through a record would drop the C# default values (the MVC binder ignores them, so take would come
    // in as 0 instead of 20) and Swagger would still list them one by one.
#pragma warning disable S107
    /// <summary>Searches the public course catalog — published courses only.</summary>
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

    /// <summary>Returns the curated set of courses shown on the landing page.</summary>
    [HttpGet("featured")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFeatured(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetFeaturedCoursesQuery(), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    /// <summary>Returns the instructor's most-used course tags, for tag-input autocomplete.</summary>
    [HttpGet("popular-tags")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> GetPopularTags(
        [FromQuery] Guid? categoryId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPopularTagsQuery(categoryId), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    /// <summary>Returns the public detail page for one course.</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCourseByIdQuery(id), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    /// <summary>Lists courses owned by the signed-in instructor.</summary>
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

    /// <summary>Lists every course platform-wide, including drafts and archived, for moderation.</summary>
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

    /// <summary>Returns the full editable representation of a course (owner or admin only).</summary>
    [HttpGet("{id:guid}/edit")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> GetForEdit(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCourseForEditByIdQuery(id), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    /// <summary>Creates a new course as an unpublished draft.</summary>
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

    /// <summary>Updates a course's title, description, pricing, category, cover image and tags.</summary>
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

    /// <summary>Publishes a draft course, making it visible in the public catalog.</summary>
    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PublishCourseCommand(id), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Takes a published course back to draft, hiding it from the public catalog.</summary>
    [HttpPost("{id:guid}/unpublish")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> Unpublish(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UnpublishCourseCommand(id), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Archives a course — a soft, reversible retirement short of deletion.</summary>
    [HttpPost("{id:guid}/archive")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ArchiveCourseCommand(id), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Reverses <see cref="Archive"/>, restoring the course to its prior published/draft state.</summary>
    [HttpPost("{id:guid}/unarchive")]
    [Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
    public async Task<IActionResult> Unarchive(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UnarchiveCourseCommand(id), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Soft-deletes a course.</summary>
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

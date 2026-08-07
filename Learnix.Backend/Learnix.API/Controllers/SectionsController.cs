using Asp.Versioning;
using Learnix.API.Extensions;
using Learnix.Application.Common.Models;
using Learnix.Application.Sections.Commands.CreateSection;
using Learnix.Application.Sections.Commands.DeleteSection;
using Learnix.Application.Sections.Commands.ReorderSections;
using Learnix.Application.Sections.Commands.UpdateSectionTitle;
using Learnix.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
// Every action here mutates course structure. Ownership is still enforced in the handler
// (course.IsOwnerOrAdmin) — this only rejects the wrong role before it gets that far.
[Authorize(Roles = $"{Roles.Instructor},{Roles.Admin}")]
[Route("api/v{version:apiVersion}")]
public sealed class SectionsController(ISender sender) : ControllerBase
{
    public sealed record CreateSectionRequest(string Title);
    public sealed record UpdateSectionTitleRequest(string Title);
    public sealed record ReorderSectionsRequest(IReadOnlyList<ReorderItem> Items);

    [HttpPost("courses/{courseId:guid}/sections")]
    public async Task<IActionResult> Create(
        Guid courseId,
        [FromBody] CreateSectionRequest body,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateSectionCommand(courseId, body.Title), cancellationToken);
        return result.ToActionResult(id => CreatedAtAction(nameof(Create), new { courseId }, new { id }));
    }

    [HttpPatch("courses/{courseId:guid}/sections/{sectionId:guid}")]
    public async Task<IActionResult> UpdateTitle(
        Guid courseId,
        Guid sectionId,
        [FromBody] UpdateSectionTitleRequest body,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateSectionTitleCommand(courseId, sectionId, body.Title), cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("courses/{courseId:guid}/sections/{sectionId:guid}")]
    public async Task<IActionResult> Delete(Guid courseId, Guid sectionId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteSectionCommand(courseId, sectionId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("courses/{courseId:guid}/sections/reorder")]
    public async Task<IActionResult> Reorder(
        Guid courseId,
        [FromBody] ReorderSectionsRequest body,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ReorderSectionsCommand(courseId, body.Items), cancellationToken);
        return result.ToActionResult();
    }
}

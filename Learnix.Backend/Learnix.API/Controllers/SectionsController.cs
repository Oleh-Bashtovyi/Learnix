using Learnix.API.Extensions;
using Learnix.Application.Common.Models;
using Learnix.Application.Sections.Commands.CreateSection;
using Learnix.Application.Sections.Commands.DeleteSection;
using Learnix.Application.Sections.Commands.ReorderSections;
using Learnix.Application.Sections.Commands.UpdateSectionTitle;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class SectionsController(ISender sender) : ControllerBase
{
    public sealed record CreateSectionRequest(string Title);
    public sealed record UpdateSectionTitleRequest(string Title);
    public sealed record ReorderSectionsRequest(IReadOnlyList<ReorderItem> Items);

    [HttpPost("courses/{courseId:guid}/sections")]
    public async Task<IActionResult> Create(
        Guid courseId,
        [FromBody] CreateSectionRequest body,
        CancellationToken ct)
    {
        var result = await sender.Send(new CreateSectionCommand(courseId, body.Title), ct);
        return result.ToActionResult(id => CreatedAtAction(nameof(Create), new { courseId }, new { id }));
    }

    [HttpPatch("courses/{courseId:guid}/sections/{sectionId:guid}")]
    public async Task<IActionResult> UpdateTitle(
        Guid courseId,
        Guid sectionId,
        [FromBody] UpdateSectionTitleRequest body,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateSectionTitleCommand(courseId, sectionId, body.Title), ct);
        return result.ToActionResult();
    }

    [HttpDelete("courses/{courseId:guid}/sections/{sectionId:guid}")]
    public async Task<IActionResult> Delete(Guid courseId, Guid sectionId, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteSectionCommand(courseId, sectionId), ct);
        return result.ToActionResult();
    }

    [HttpPost("courses/{courseId:guid}/sections/reorder")]
    public async Task<IActionResult> Reorder(
        Guid courseId,
        [FromBody] ReorderSectionsRequest body,
        CancellationToken ct)
    {
        var result = await sender.Send(new ReorderSectionsCommand(courseId, body.Items), ct);
        return result.ToActionResult();
    }
}

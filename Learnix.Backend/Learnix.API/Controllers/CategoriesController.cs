using Asp.Versioning;
using Learnix.API.Extensions;
using Learnix.Application.Categories.Commands.CreateCategory;
using Learnix.Application.Categories.Commands.DeleteCategory;
using Learnix.Application.Categories.Commands.UpdateCategory;
using Learnix.Application.Categories.Queries.GetAdminCategories;
using Learnix.Application.Categories.Queries.GetAllCategories;
using Learnix.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class CategoriesController(ISender sender) : ControllerBase
{
    public sealed record CreateCategoryRequest(string Name, string Slug, string? ImageBlobPath);
    public sealed record UpdateCategoryRequest(string Name, string Slug, string? ImageBlobPath, bool RemoveImage);

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllCategoriesQuery(), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpGet("admin")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAdminCategoriesQuery(), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest body, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateCategoryCommand(body.Name, body.Slug, body.ImageBlobPath), cancellationToken);
        return result.ToActionResult(onSuccess: id => CreatedAtAction(nameof(GetAllForAdmin), new { }, new { id }));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest body, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateCategoryCommand(id, body.Name, body.Slug, body.ImageBlobPath, body.RemoveImage), cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteCategoryCommand(id), cancellationToken);
        return result.ToActionResult();
    }
}

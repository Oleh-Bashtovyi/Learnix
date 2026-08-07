using Asp.Versioning;
using Learnix.API.Extensions;
using Learnix.Application.Wishlist.Commands.AddToWishlist;
using Learnix.Application.Wishlist.Commands.RemoveFromWishlist;
using Learnix.Application.Wishlist.Queries.GetMyWishlist;
using Learnix.Application.Wishlist.Queries.GetWishlistCount;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class WishlistController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMine(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetMyWishlistQuery(skip, take), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetCount(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetWishlistCountQuery(), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpPost("{courseId:guid}")]
    public async Task<IActionResult> Add(Guid courseId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AddToWishlistCommand(courseId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{courseId:guid}")]
    public async Task<IActionResult> Remove(Guid courseId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveFromWishlistCommand(courseId), cancellationToken);
        return result.ToActionResult();
    }
}

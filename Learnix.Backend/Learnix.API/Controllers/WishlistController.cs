using Learnix.API.Extensions;
using Learnix.Application.Wishlist.Commands.AddToWishlist;
using Learnix.Application.Wishlist.Commands.RemoveFromWishlist;
using Learnix.Application.Wishlist.Queries.GetMyWishlist;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class WishlistController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMine(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetMyWishlistQuery(skip, take), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpPost("{courseId:guid}")]
    public async Task<IActionResult> Add(Guid courseId, CancellationToken ct)
    {
        var result = await sender.Send(new AddToWishlistCommand(courseId), ct);
        return result.ToActionResult();
    }

    [HttpDelete("{courseId:guid}")]
    public async Task<IActionResult> Remove(Guid courseId, CancellationToken ct)
    {
        var result = await sender.Send(new RemoveFromWishlistCommand(courseId), ct);
        return result.ToActionResult();
    }
}

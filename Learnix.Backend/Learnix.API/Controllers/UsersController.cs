using Learnix.API.Extensions;
using Learnix.Application.Users.Commands.UpdateProfile;
using Learnix.Application.Users.Queries.GetMyProfile;
using Learnix.Application.Users.Queries.GetUserProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(ISender sender) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyProfileQuery(), ct);
        return result.ToActionResult();
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.ToActionResult();
    }

    [HttpGet("{userId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserProfile(Guid userId, CancellationToken ct)
    {
        var result = await sender.Send(new GetUserProfileQuery(userId), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }
}

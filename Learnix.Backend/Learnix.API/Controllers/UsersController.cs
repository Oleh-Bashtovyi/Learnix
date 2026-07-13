using Learnix.API.Extensions;
using Learnix.Application.Users.Commands.UpdateProfile;
using Learnix.Application.Users.Queries.GetInstructorProfile;
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
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyProfileQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{userId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserProfile(Guid userId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUserProfileQuery(userId), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    /// <summary>The public profile plus what the instructor's published courses add up to.</summary>
    [HttpGet("{userId:guid}/instructor-profile")]
    [AllowAnonymous]
    public async Task<IActionResult> GetInstructorProfile(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInstructorProfileQuery(userId), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }
}

using Learnix.API.Extensions;
using Learnix.Application.Achievements.Commands.MarkAchievementSeen;
using Learnix.Application.Achievements.Queries.GetMyAchievements;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[Route("api/achievements")]
[Authorize]
public sealed class AchievementsController(ISender sender) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMyAchievements(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyAchievementsQuery(), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpPost("{achievementId:guid}/seen")]
    public async Task<IActionResult> MarkSeen(Guid achievementId, CancellationToken ct)
    {
        var result = await sender.Send(new MarkAchievementSeenCommand(achievementId), ct);
        return result.ToActionResult();
    }
}

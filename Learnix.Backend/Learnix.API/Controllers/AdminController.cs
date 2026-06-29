using Learnix.API.Extensions;
using Learnix.Application.Admin.Queries.GetAdminStats;
using Learnix.Application.Courses.Commands.AdminDeleteCourse;
using Learnix.Application.Courses.Commands.AdminPublishCourse;
using Learnix.Application.Courses.Commands.AdminRecoverCourse;
using Learnix.Application.Courses.Commands.AdminUnpublishCourse;
using Learnix.Application.Courses.Queries.GetAdminCourses;
using Learnix.Application.Payments.Queries.GetAdminPayments;
using Learnix.Application.Users.Commands.AdminAssignRole;
using Learnix.Application.Users.Commands.AdminBanUser;
using Learnix.Application.Users.Commands.AdminDeleteUser;
using Learnix.Application.Users.Commands.AdminRecoverUser;
using Learnix.Application.Users.Commands.AdminRemoveRole;
using Learnix.Application.Users.Commands.AdminUnbanUser;
using Learnix.Application.Users.Queries.GetAdminUsers;
using Learnix.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminController(ISender sender) : ControllerBase
{
    // Stats 

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminStatsQuery(), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    // Users 

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAdminUsersQuery(search, skip, take, includeDeleted), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpPost("users/{userId:guid}/ban")]
    public async Task<IActionResult> BanUser(Guid userId, CancellationToken ct)
    {
        var result = await sender.Send(new AdminBanUserCommand(userId), ct);
        return result.ToActionResult();
    }

    [HttpPost("users/{userId:guid}/unban")]
    public async Task<IActionResult> UnbanUser(Guid userId, CancellationToken ct)
    {
        var result = await sender.Send(new AdminUnbanUserCommand(userId), ct);
        return result.ToActionResult();
    }

    [HttpDelete("users/{userId:guid}")]
    public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken ct)
    {
        var result = await sender.Send(new AdminDeleteUserCommand(userId), ct);
        return result.ToActionResult();
    }

    [HttpPost("users/{userId:guid}/recover")]
    public async Task<IActionResult> RecoverUser(Guid userId, CancellationToken ct)
    {
        var result = await sender.Send(new AdminRecoverUserCommand(userId), ct);
        return result.ToActionResult();
    }

    [HttpPost("users/{userId:guid}/roles/{role}")]
    public async Task<IActionResult> AssignRole(Guid userId, string role, CancellationToken ct)
    {
        var result = await sender.Send(new AdminAssignRoleCommand(userId, role), ct);
        return result.ToActionResult();
    }

    [HttpDelete("users/{userId:guid}/roles/{role}")]
    public async Task<IActionResult> RemoveRole(Guid userId, string role, CancellationToken ct)
    {
        var result = await sender.Send(new AdminRemoveRoleCommand(userId, role), ct);
        return result.ToActionResult();
    }

    // Courses 

    [HttpGet("courses")]
    public async Task<IActionResult> GetCourses(
        [FromQuery] string? search,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAdminCoursesQuery(search, skip, take, categoryId, includeDeleted), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpPost("courses/{courseId:guid}/publish")]
    public async Task<IActionResult> PublishCourse(Guid courseId, CancellationToken ct)
    {
        var result = await sender.Send(new AdminPublishCourseCommand(courseId), ct);
        return result.ToActionResult();
    }

    [HttpPost("courses/{courseId:guid}/unpublish")]
    public async Task<IActionResult> UnpublishCourse(Guid courseId, CancellationToken ct)
    {
        var result = await sender.Send(new AdminUnpublishCourseCommand(courseId), ct);
        return result.ToActionResult();
    }

    [HttpDelete("courses/{courseId:guid}")]
    public async Task<IActionResult> DeleteCourse(Guid courseId, CancellationToken ct)
    {
        var result = await sender.Send(new AdminDeleteCourseCommand(courseId), ct);
        return result.ToActionResult();
    }

    [HttpPost("courses/{courseId:guid}/recover")]
    public async Task<IActionResult> RecoverCourse(Guid courseId, CancellationToken ct)
    {
        var result = await sender.Send(new AdminRecoverCourseCommand(courseId), ct);
        return result.ToActionResult();
    }

    // Payments 

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments(
        [FromQuery] string? search,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAdminPaymentsQuery(search, skip, take), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }
}

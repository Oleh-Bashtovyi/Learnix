using Asp.Versioning;
using Learnix.API.Constants;
using Learnix.API.Extensions;
using Learnix.Application.InstructorApplications.Commands.ApproveApplication;
using Learnix.Application.InstructorApplications.Commands.RejectApplication;
using Learnix.Application.InstructorApplications.Commands.SubmitApplication;
using Learnix.Application.InstructorApplications.Queries.GetMyApplication;
using Learnix.Application.InstructorApplications.Queries.GetPendingApplications;
using Learnix.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learnix.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/instructor-applications")]
[Authorize]
public sealed class InstructorApplicationsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthPolicies.EmailConfirmed)]
    public async Task<IActionResult> Submit([FromBody] SubmitApplicationCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult(onSuccess: id => Created(string.Empty, new { id }));
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyApplicationQuery(), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpGet("pending")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetPending(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetPendingApplicationsQuery(skip, take), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ApproveApplicationCommand(id), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectApplicationRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RejectApplicationCommand(id, request.RejectionReason), cancellationToken);
        return result.ToActionResult();
    }
}

public record RejectApplicationRequest(string? RejectionReason);

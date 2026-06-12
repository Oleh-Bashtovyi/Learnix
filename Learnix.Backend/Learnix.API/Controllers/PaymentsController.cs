using Learnix.API.Extensions;
using Learnix.Application.Payments.Commands.InitiateMockPayment;
using Learnix.Application.Payments.Queries.GetMyPayments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Learnix.API.RateLimiting;

namespace Learnix.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PaymentsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Payments)]
    [Authorize(Policy = "EmailConfirmed")]
    public async Task<IActionResult> InitiatePayment(
        [FromBody] InitiateMockPaymentCommand command,
        CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetMyPaymentsQuery(skip, take), ct);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }
}

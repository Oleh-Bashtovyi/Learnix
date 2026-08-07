using Asp.Versioning;
using Learnix.API.Constants;
using Learnix.API.Extensions;
using Learnix.API.RateLimiting;
using Learnix.Application.Payments.Commands.InitiateMockPayment;
using Learnix.Application.Payments.Queries.GetMyPayments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Learnix.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class PaymentsController(ISender sender) : ControllerBase
{
    /// <summary>Initiates a mock payment for a course — there is no real payment gateway behind this.</summary>
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Payments)]
    [Authorize(Policy = AuthPolicies.EmailConfirmed)]
    public async Task<IActionResult> InitiatePayment(
        [FromBody] InitiateMockPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }

    /// <summary>Lists the signed-in user's payment history.</summary>
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetMyPaymentsQuery(skip, take), cancellationToken);
        return result.ToActionResult(onSuccess: value => Ok(value));
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeNator.Api.Services;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/distraction-events")]
public class DistractionEventsController(DistractionService distractions) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> AddBatch(List<DistractionEventRequest> events,
        CancellationToken cancellationToken) =>
        this.ToNoContent(await distractions.AddBatchAsync(User.GetUserId(), events, cancellationToken));

    [HttpGet]
    public async Task<ActionResult<List<DistractionEventResponse>>> List(
        [FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
    {
        if (to <= from || to - from > TimeSpan.FromDays(366))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid date range.");
        return await distractions.ListAsync(User.GetUserId(), from, to, cancellationToken);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeNator.Api.Services;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/daily-review")]
public class DailyReviewController(DailyReviewService review) : ControllerBase
{
    /// <param name="offsetMinutes">The client's UTC offset in minutes, so the day is the user's local day.</param>
    [HttpGet]
    public async Task<ActionResult<DailyReviewResponse>> Get([FromQuery] DateOnly date,
        [FromQuery] int offsetMinutes, CancellationToken cancellationToken)
    {
        if (offsetMinutes is < -14 * 60 or > 14 * 60)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid UTC offset.");
        return await review.GetAsync(User.GetUserId(), date, TimeSpan.FromMinutes(offsetMinutes), cancellationToken);
    }
}

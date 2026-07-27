using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeNator.Api.Services;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/leaderboard")]
public class LeaderboardController(LeaderboardService leaderboard, TimeProvider clock) : ControllerBase
{
    /// <summary>The top 50 for a UTC date, highest first. Defaults to today.</summary>
    [HttpGet]
    public Task<List<LeaderboardEntry>> Get([FromQuery] DateOnly? date, CancellationToken cancellationToken) =>
        leaderboard.GetTopAsync(date ?? LeaderboardService.DayOf(clock.GetUtcNow()), cancellationToken);
}

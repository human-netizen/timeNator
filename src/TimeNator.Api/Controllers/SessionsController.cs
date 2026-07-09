using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeNator.Api.Services;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/sessions")]
public class SessionsController(SessionService sessions) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SessionResponse>>> List(
        [FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
    {
        if (to <= from || to - from > TimeSpan.FromDays(366))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid date range.");

        return await sessions.ListAsync(User.GetUserId(), from, to, cancellationToken);
    }

    [HttpPost]
    public async Task<ActionResult<SessionResponse>> Create(
        CreateSessionRequest request, CancellationToken cancellationToken)
    {
        var result = await sessions.CreateAsync(User.GetUserId(), request, cancellationToken);
        if (result.Session is null)
        {
            foreach (var violation in result.Violations)
                ModelState.AddModelError(violation.Field, violation.Message);
            return ValidationProblem(ModelState);
        }

        return Created($"/api/sessions/{result.Session.Id}", result.Session);
    }
}

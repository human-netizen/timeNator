using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeNator.Api.Services;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/day-offs")]
public class DayOffsController(DayOffService dayOffs) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<DayOffResponse>>> List(
        [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken cancellationToken)
    {
        if (to < from || to.DayNumber - from.DayNumber > 366)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid date range.");

        return await dayOffs.ListAsync(User.GetUserId(), from, to, cancellationToken);
    }

    [HttpPost]
    public async Task<ActionResult<DayOffResponse>> Create(DayOffRequest request, CancellationToken cancellationToken)
    {
        if (request.Note is { Length: > 200 })
        {
            ModelState.AddModelError(nameof(request.Note), "The note must be at most 200 characters.");
            return ValidationProblem(ModelState);
        }

        try
        {
            var created = await dayOffs.CreateAsync(User.GetUserId(), request, cancellationToken);
            return Created($"/api/day-offs/{created.Date:yyyy-MM-dd}", created);
        }
        catch (DbUpdateException)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "That day is already a day off.");
        }
    }

    [HttpDelete("{date}")]
    public async Task<IActionResult> Delete(DateOnly date, CancellationToken cancellationToken) =>
        await dayOffs.DeleteAsync(User.GetUserId(), date, cancellationToken) ? NoContent() : NotFound();
}

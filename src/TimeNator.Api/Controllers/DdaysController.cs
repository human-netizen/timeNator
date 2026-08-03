using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeNator.Api.Services;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ddays")]
public class DdaysController(DdayService ddays) : ControllerBase
{
    [HttpGet]
    public Task<List<DdayResponse>> List(CancellationToken cancellationToken) =>
        ddays.ListAsync(User.GetUserId(), cancellationToken);

    [HttpPost]
    public async Task<ActionResult<DdayResponse>> Create(DdayRequest request, CancellationToken cancellationToken)
    {
        if (request.Title?.Trim().Length is not (> 0 and <= 100))
        {
            ModelState.AddModelError(nameof(request.Title), "The title must be 1 to 100 characters.");
            return ValidationProblem(ModelState);
        }
        var created = await ddays.CreateAsync(User.GetUserId(), request, cancellationToken);
        return Created($"/api/ddays/{created.Id}", created);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await ddays.DeleteAsync(User.GetUserId(), id, cancellationToken) ? NoContent() : NotFound();
}

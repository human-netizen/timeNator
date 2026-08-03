using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeNator.Api.Services;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/timetable")]
public class TimetableController(TimetableService timetable) : ControllerBase
{
    [HttpGet]
    public Task<List<TimetableResponse>> List(CancellationToken cancellationToken) =>
        timetable.ListAsync(User.GetUserId(), cancellationToken);

    [HttpPost]
    public async Task<ActionResult<TimetableResponse>> Create(TimetableRequest request,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await timetable.CreateAsync(User.GetUserId(), request, cancellationToken),
            created => Created($"/api/timetable/{created.Id}", created));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TimetableResponse>> Update(Guid id, TimetableRequest request,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await timetable.UpdateAsync(User.GetUserId(), id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        this.ToNoContent(await timetable.DeleteAsync(User.GetUserId(), id, cancellationToken));
}

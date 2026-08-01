using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeNator.Api.Services;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/allowed-apps")]
public class AllowedAppsController(AllowedAppService apps) : ControllerBase
{
    [HttpGet]
    public Task<List<AllowedAppResponse>> List(CancellationToken cancellationToken) =>
        apps.ListAsync(User.GetUserId(), cancellationToken);

    [HttpPost]
    public async Task<ActionResult<AllowedAppResponse>> Add(AllowedAppRequest request,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await apps.AddAsync(User.GetUserId(), request, cancellationToken),
            added => Created($"/api/allowed-apps/{added.Id}", added));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken cancellationToken) =>
        this.ToNoContent(await apps.RemoveAsync(User.GetUserId(), id, cancellationToken));
}

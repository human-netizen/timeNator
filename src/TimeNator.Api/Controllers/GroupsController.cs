using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeNator.Api.Services;
using TimeNator.Api.Validation;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/groups")]
public class GroupsController(GroupService groups) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<GroupDetail>> Create(
        CreateGroupRequest request, IValidator<CreateGroupRequest> validator, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationProblem(validation.ToModelState());

        var result = await groups.CreateAsync(User.GetUserId(), request, cancellationToken);
        return this.ToActionResult(result, created => Created($"/api/groups/{created.Id}", created));
    }

    [HttpGet("mine")]
    public Task<List<GroupSummary>> Mine(CancellationToken cancellationToken) =>
        groups.ListMineAsync(User.GetUserId(), cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GroupDetail>> Get(Guid id, CancellationToken cancellationToken) =>
        this.ToActionResult(await groups.GetAsync(User.GetUserId(), id, cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        this.ToNoContent(await groups.DeleteAsync(User.GetUserId(), id, cancellationToken));

    [HttpPost("{id:guid}/join")]
    public async Task<ActionResult<GroupDetail>> Join(Guid id, JoinGroupRequest request,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await groups.JoinAsync(User.GetUserId(), id, request.Password, cancellationToken));

    [HttpPost("{id:guid}/leave")]
    public async Task<IActionResult> Leave(Guid id, CancellationToken cancellationToken) =>
        this.ToNoContent(await groups.LeaveAsync(User.GetUserId(), id, cancellationToken));

    [HttpGet("{id:guid}/members")]
    public async Task<ActionResult<List<GroupMemberItem>>> Members(Guid id, CancellationToken cancellationToken) =>
        this.ToActionResult(await groups.ListMembersAsync(User.GetUserId(), id, cancellationToken));
}

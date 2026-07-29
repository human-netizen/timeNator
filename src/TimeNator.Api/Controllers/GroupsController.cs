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
public class GroupsController(
    GroupService groups,
    InviteService invites,
    PresenceService presence,
    ChatService chat) : ControllerBase
{
    /// <summary>Who in the group is studying right now, for the first paint of the live view.</summary>
    [HttpGet("{id:guid}/presence")]
    public async Task<ActionResult<List<MemberPresence>>> Presence(Guid id, CancellationToken cancellationToken)
    {
        var members = await groups.ListMemberIdsAsync(User.GetUserId(), id, cancellationToken);
        if (members.Error is not null)
            return this.ToProblem(members);
        return await presence.GetManyAsync(members.Value!);
    }

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

    [HttpGet("search")]
    public Task<PagedResult<GroupSummary>> Search([FromQuery] string? q, [FromQuery] int page,
        CancellationToken cancellationToken) =>
        groups.SearchAsync(q, page, cancellationToken);

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

    [HttpPost("{id:guid}/invites")]
    public async Task<ActionResult<InviteResponse>> CreateInvite(Guid id, CreateInviteRequest request,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await invites.CreateAsync(User.GetUserId(), id, request, cancellationToken),
            invite => Created($"/api/groups/join/{invite.Code}", invite));

    [HttpGet("join/{code}")]
    public async Task<ActionResult<GroupSummary>> PreviewInvite(string code, CancellationToken cancellationToken) =>
        this.ToActionResult(await invites.PreviewAsync(code, cancellationToken));

    [HttpPost("join/{code}")]
    public async Task<ActionResult<GroupDetail>> AcceptInvite(string code, CancellationToken cancellationToken) =>
        this.ToActionResult(await invites.AcceptAsync(User.GetUserId(), code, cancellationToken));

    [HttpGet("{id:guid}/messages")]
    public async Task<ActionResult<List<GroupMessageItem>>> Messages(Guid id, [FromQuery] Guid? before,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await chat.HistoryAsync(User.GetUserId(), id, before, cancellationToken));

    [HttpGet("{id:guid}/members")]
    public async Task<ActionResult<List<GroupMemberItem>>> Members(Guid id, CancellationToken cancellationToken) =>
        this.ToActionResult(await groups.ListMembersAsync(User.GetUserId(), id, cancellationToken));
}

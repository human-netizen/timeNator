using System.Net;
using System.Net.Http.Json;
using TimeNator.Shared;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Tests;

[Collection(ApiCollection.Name)]
public class GroupTests(ApiFactory factory)
{
    [Fact]
    public async Task Creator_is_the_owner_and_sees_the_group_in_mine()
    {
        var owner = await factory.CreateUserClientAsync();

        var group = await CreateGroupAsync(owner, isPublic: true);
        var mine = await owner.GetFromJsonAsync<List<GroupSummary>>("/api/groups/mine");

        Assert.Equal(GroupRole.Owner, group.MyRole);
        Assert.Contains(mine!, g => g.Id == group.Id);
    }

    [Fact]
    public async Task Joining_a_public_group_needs_no_password()
    {
        var owner = await factory.CreateUserClientAsync();
        var member = await factory.CreateUserClientAsync();
        var group = await CreateGroupAsync(owner, isPublic: true);

        var join = await member.PostAsJsonAsync($"/api/groups/{group.Id}/join", new JoinGroupRequest(null));
        var again = await member.PostAsJsonAsync($"/api/groups/{group.Id}/join", new JoinGroupRequest(null));
        var members = await owner.GetFromJsonAsync<List<GroupMemberItem>>($"/api/groups/{group.Id}/members");

        Assert.Equal(HttpStatusCode.OK, join.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(2, members!.Count);
    }

    [Fact]
    public async Task Private_group_checks_the_password_and_stays_hidden()
    {
        var owner = await factory.CreateUserClientAsync();
        var outsider = await factory.CreateUserClientAsync();
        var group = await CreateGroupAsync(owner, isPublic: false, password: "open-sesame");

        var peek = await outsider.GetAsync($"/api/groups/{group.Id}");
        var wrong = await outsider.PostAsJsonAsync($"/api/groups/{group.Id}/join", new JoinGroupRequest("nope"));
        var right = await outsider.PostAsJsonAsync($"/api/groups/{group.Id}/join",
            new JoinGroupRequest("open-sesame"));

        Assert.Equal(HttpStatusCode.NotFound, peek.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.OK, right.StatusCode);
    }

    [Fact]
    public async Task Owner_cannot_leave_but_a_member_can()
    {
        var owner = await factory.CreateUserClientAsync();
        var member = await factory.CreateUserClientAsync();
        var group = await CreateGroupAsync(owner, isPublic: true);
        await member.PostAsJsonAsync($"/api/groups/{group.Id}/join", new JoinGroupRequest(null));

        var ownerLeave = await owner.PostAsync($"/api/groups/{group.Id}/leave", null);
        var memberLeave = await member.PostAsync($"/api/groups/{group.Id}/leave", null);

        Assert.Equal(HttpStatusCode.BadRequest, ownerLeave.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, memberLeave.StatusCode);
    }

    [Fact]
    public async Task Only_the_owner_can_delete()
    {
        var owner = await factory.CreateUserClientAsync();
        var member = await factory.CreateUserClientAsync();
        var group = await CreateGroupAsync(owner, isPublic: true);
        await member.PostAsJsonAsync($"/api/groups/{group.Id}/join", new JoinGroupRequest(null));

        var byMember = await member.DeleteAsync($"/api/groups/{group.Id}");
        var byOwner = await owner.DeleteAsync($"/api/groups/{group.Id}");

        Assert.Equal(HttpStatusCode.NotFound, byMember.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, byOwner.StatusCode);
    }

    internal static async Task<GroupDetail> CreateGroupAsync(HttpClient client, bool isPublic,
        string? password = null, string name = "Study Hall")
    {
        var response = await client.PostAsJsonAsync("/api/groups",
            new CreateGroupRequest(name, "Quiet focus", isPublic, password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GroupDetail>())!;
    }
}

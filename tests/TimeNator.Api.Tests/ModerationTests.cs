using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Tests;

[Collection(ApiCollection.Name)]
public class ModerationTests(ApiFactory factory)
{
    [Fact]
    public async Task Owner_updates_settings_and_members_cannot()
    {
        var (owner, member, group) = await GroupWithMemberAsync();
        var update = new UpdateGroupRequest("Renamed", "d", "Exams on Friday", false, 3600);

        var byMember = await member.Client.PutAsJsonAsync($"/api/groups/{group.Id}", update);
        var byOwner = await owner.Client.PutAsJsonAsync($"/api/groups/{group.Id}", update);
        var detail = await byOwner.Content.ReadFromJsonAsync<GroupDetail>();

        Assert.Equal(HttpStatusCode.NotFound, byMember.StatusCode);
        Assert.Equal("Exams on Friday", detail!.Announcement);
        Assert.False(detail.ChatEnabled);
        Assert.Equal(3600, detail.MinDailySeconds);
    }

    [Fact]
    public async Task Muted_member_cannot_post_but_the_owner_can()
    {
        var (owner, member, group) = await GroupWithMemberAsync();
        await owner.Client.PutAsJsonAsync($"/api/groups/{group.Id}/members/{member.Auth.UserId}/chat-permission",
            new ChatPermissionRequest(false));
        await using var memberHub = await factory.ConnectHubAsync(member);
        await using var ownerHub = await factory.ConnectHubAsync(owner);

        await Assert.ThrowsAsync<HubException>(() => memberHub.InvokeAsync("SendMessage", group.Id, "hi"));
        await ownerHub.InvokeAsync("SendMessage", group.Id, "owners may always post");
    }

    [Fact]
    public async Task Kicked_member_can_rejoin_but_blacklisted_one_cannot()
    {
        var (owner, member, group) = await GroupWithMemberAsync();

        var kick = await owner.Client.PostAsync($"/api/groups/{group.Id}/members/{member.Auth.UserId}/kick", null);
        var rejoin = await member.Client.PostAsJsonAsync($"/api/groups/{group.Id}/join", new JoinGroupRequest(null));
        var ban = await owner.Client.PostAsJsonAsync($"/api/groups/{group.Id}/members/{member.Auth.UserId}/blacklist",
            new RemoveMemberRequest("spam"));
        var afterBan = await member.Client.PostAsJsonAsync($"/api/groups/{group.Id}/join", new JoinGroupRequest(null));

        Assert.Equal(HttpStatusCode.NoContent, kick.StatusCode);
        Assert.Equal(HttpStatusCode.OK, rejoin.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, ban.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, afterBan.StatusCode);
    }

    [Fact]
    public async Task Owner_cannot_kick_themselves()
    {
        var (owner, _, group) = await GroupWithMemberAsync();

        var response = await owner.Client.PostAsync($"/api/groups/{group.Id}/members/{owner.Auth.UserId}/kick", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<(TestUser Owner, TestUser Member, GroupDetail Group)> GroupWithMemberAsync()
    {
        var owner = await factory.CreateUserAsync("Owner");
        var member = await factory.CreateUserAsync("Member");
        var group = await GroupTests.CreateGroupAsync(owner.Client, isPublic: true);
        await member.Client.PostAsJsonAsync($"/api/groups/{group.Id}/join", new JoinGroupRequest(null));
        return (owner, member, group);
    }
}

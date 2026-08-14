using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Tests;

[Collection(ApiCollection.Name)]
public class ChatTests(ApiFactory factory)
{
    [Fact]
    public async Task Message_is_broadcast_to_members_and_kept_in_history()
    {
        var alice = await factory.CreateUserAsync("Alice");
        var bob = await factory.CreateUserAsync("Bob");
        var group = await GroupTests.CreateGroupAsync(alice.Client, isPublic: true);
        await bob.Client.PostAsJsonAsync($"/api/groups/{group.Id}/join", new JoinGroupRequest(null));

        var received = new TaskCompletionSource<GroupMessageItem>();
        await using var bobHub = await factory.ConnectHubAsync(bob);
        bobHub.On<Guid, GroupMessageItem>("MessageReceived", (_, m) => received.TrySetResult(m));
        await using var aliceHub = await factory.ConnectHubAsync(alice);

        await aliceHub.InvokeAsync("SendMessage", group.Id, "  hello, study hall  ");
        var message = await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var history = await bob.Client.GetFromJsonAsync<List<GroupMessageItem>>($"/api/groups/{group.Id}/messages");

        Assert.Equal("hello, study hall", message.Body);
        Assert.Equal("Alice", message.DisplayName);
        Assert.Equal(message.Id, Assert.Single(history!).Id);
    }

    [Fact]
    public async Task History_pages_backwards_with_a_cursor()
    {
        var alice = await factory.CreateUserAsync("Alice");
        var group = await GroupTests.CreateGroupAsync(alice.Client, isPublic: true);
        await using var hub = await factory.ConnectHubAsync(alice);
        for (var i = 0; i < 55; i++)
            await hub.InvokeAsync("SendMessage", group.Id, $"message {i}");

        var first = (await alice.Client.GetFromJsonAsync<List<GroupMessageItem>>($"/api/groups/{group.Id}/messages"))!;
        var second = await alice.Client.GetFromJsonAsync<List<GroupMessageItem>>(
            $"/api/groups/{group.Id}/messages?before={first.Last().Id}");

        Assert.Equal(50, first.Count);
        Assert.Equal("message 54", first[0].Body);
        Assert.Equal(5, second!.Count);
        Assert.Equal("message 0", second.Last().Body);
    }

    [Fact]
    public async Task Outsiders_can_neither_post_nor_read()
    {
        var alice = await factory.CreateUserAsync("Alice");
        var mallory = await factory.CreateUserAsync("Mallory");
        var group = await GroupTests.CreateGroupAsync(alice.Client, isPublic: true);
        await using var hub = await factory.ConnectHubAsync(mallory);

        await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("SendMessage", group.Id, "let me in"));
        var read = await mallory.Client.GetAsync($"/api/groups/{group.Id}/messages");

        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
    }
}

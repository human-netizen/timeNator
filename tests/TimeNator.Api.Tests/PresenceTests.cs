using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using TimeNator.Shared;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Tests;

[Collection(ApiCollection.Name)]
public class PresenceTests(ApiFactory factory)
{
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Starting_and_stopping_reaches_the_other_members_and_the_presence_endpoint()
    {
        var alice = await factory.CreateUserAsync("Alice");
        var bob = await factory.CreateUserAsync("Bob");
        var group = await GroupTests.CreateGroupAsync(alice.Client, isPublic: true);
        await bob.Client.PostAsJsonAsync($"/api/groups/{group.Id}/join", new JoinGroupRequest(null));
        var subject = await (await alice.Client.PostAsJsonAsync("/api/subjects",
            new CreateSubjectRequest("Math", "#FF0000"))).Content.ReadFromJsonAsync<SubjectResponse>();

        var started = new TaskCompletionSource<MemberPresence>();
        var stopped = new TaskCompletionSource<Guid>();
        await using var bobHub = await factory.ConnectHubAsync(bob);
        bobHub.On<Guid, MemberPresence>("MemberStarted", (_, p) => started.TrySetResult(p));
        bobHub.On<Guid, Guid>("MemberStopped", (_, userId) => stopped.TrySetResult(userId));
        await using var aliceHub = await factory.ConnectHubAsync(alice);

        await aliceHub.InvokeAsync("StartedStudying", subject!.Id, SessionMode.Pomodoro);
        var presence = await started.Task.WaitAsync(Wait);
        var live = await bob.Client.GetFromJsonAsync<List<MemberPresence>>($"/api/groups/{group.Id}/presence");

        await aliceHub.InvokeAsync("StoppedStudying");
        var stoppedUser = await stopped.Task.WaitAsync(Wait);
        var afterStop = await bob.Client.GetFromJsonAsync<List<MemberPresence>>($"/api/groups/{group.Id}/presence");

        Assert.Equal("Alice", presence.DisplayName);
        Assert.Equal("Math", presence.SubjectName);
        Assert.Equal(SessionMode.Pomodoro, presence.Mode);
        Assert.Equal(alice.Auth.UserId, Assert.Single(live!).UserId);
        Assert.Equal(alice.Auth.UserId, stoppedUser);
        Assert.Empty(afterStop!);
    }

    [Fact]
    public async Task Disconnecting_clears_presence()
    {
        var alice = await factory.CreateUserAsync("Alice");
        var group = await GroupTests.CreateGroupAsync(alice.Client, isPublic: true);
        var subject = await (await alice.Client.PostAsJsonAsync("/api/subjects",
            new CreateSubjectRequest("Math", "#FF0000"))).Content.ReadFromJsonAsync<SubjectResponse>();

        var hub = await factory.ConnectHubAsync(alice);
        await hub.InvokeAsync("StartedStudying", subject!.Id, SessionMode.Stopwatch);
        await hub.DisposeAsync();

        var presence = await alice.Client.GetFromJsonAsync<List<MemberPresence>>($"/api/groups/{group.Id}/presence");
        Assert.Empty(presence!);
    }
}

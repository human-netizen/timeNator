using System.Net;
using System.Net.Http.Json;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Tests;

[Collection(ApiCollection.Name)]
public class FocusTests(ApiFactory factory)
{
    [Fact]
    public async Task Allowed_apps_are_normalised_unique_and_private()
    {
        var alice = await factory.CreateUserClientAsync();
        var bob = await factory.CreateUserClientAsync();

        var added = await alice.PostAsJsonAsync("/api/allowed-apps", new AllowedAppRequest("Code.EXE", "VS Code"));
        var duplicate = await alice.PostAsJsonAsync("/api/allowed-apps", new AllowedAppRequest("code", "Again"));
        var app = await added.Content.ReadFromJsonAsync<AllowedAppResponse>();
        var bobsDelete = await bob.DeleteAsync($"/api/allowed-apps/{app!.Id}");
        var list = await alice.GetFromJsonAsync<List<AllowedAppResponse>>("/api/allowed-apps");

        Assert.Equal("code", app.ProcessName);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, bobsDelete.StatusCode);
        Assert.Single(list!);
    }

    [Fact]
    public async Task Distraction_batch_is_stored_and_foreign_session_ids_are_dropped()
    {
        var alice = await factory.CreateUserClientAsync();
        var bob = await factory.CreateUserClientAsync();
        var now = DateTimeOffset.UtcNow;
        var bobsSession = Guid.CreateVersion7();

        var post = await alice.PostAsJsonAsync("/api/distraction-events", new List<DistractionEventRequest>
        {
            new(null, "MSEdge", "News", now.AddMinutes(-20), 45),
            new(bobsSession, "discord", "Chat", now.AddMinutes(-10), 120)
        });
        var mine = await alice.GetFromJsonAsync<List<DistractionEventResponse>>(
            $"/api/distraction-events?from={Uri.EscapeDataString(now.AddHours(-1).ToString("O"))}" +
            $"&to={Uri.EscapeDataString(now.AddMinutes(1).ToString("O"))}");
        var bobs = await bob.GetFromJsonAsync<List<DistractionEventResponse>>(
            $"/api/distraction-events?from={Uri.EscapeDataString(now.AddHours(-1).ToString("O"))}" +
            $"&to={Uri.EscapeDataString(now.AddMinutes(1).ToString("O"))}");

        Assert.Equal(HttpStatusCode.NoContent, post.StatusCode);
        Assert.Equal(2, mine!.Count);
        Assert.Equal("discord", mine[0].ProcessName);
        Assert.Null(mine[0].SessionId);
        Assert.Equal("msedge", mine[1].ProcessName);
        Assert.Empty(bobs!);
    }
}

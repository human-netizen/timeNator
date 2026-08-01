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
}

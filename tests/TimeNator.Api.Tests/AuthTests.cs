using System.Net;
using System.Net.Http.Json;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Tests;

[Collection(ApiCollection.Name)]
public class AuthTests(ApiFactory factory)
{
    [Fact]
    public async Task Register_then_login_returns_tokens()
    {
        var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@test.dev";

        var register = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "password123", "Ada"));
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "password123"));

        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal("Ada", auth!.DisplayName);
        Assert.False(string.IsNullOrEmpty(auth.AccessToken));
        Assert.False(string.IsNullOrEmpty(auth.RefreshToken));
    }

    [Fact]
    public async Task Wrong_password_is_unauthorized()
    {
        var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@test.dev";
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "password123", "Ada"));

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Invalid_registration_returns_field_errors()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("nope", "x", ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Email", body);
        Assert.Contains("Password", body);
    }

    [Fact]
    public async Task Refresh_token_stops_working_after_logout()
    {
        var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@test.dev";
        var registered = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "password123", "Ada"));
        var auth = (await registered.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new("Bearer", auth.AccessToken);

        var refreshed = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(auth.RefreshToken));
        await client.PostAsJsonAsync("/api/auth/logout", new RefreshRequest(auth.RefreshToken));
        var afterLogout = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(auth.RefreshToken));

        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task Refresh_rotates_and_reusing_an_old_token_revokes_everything()
    {
        var client = factory.CreateClient();
        var registered = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest($"{Guid.NewGuid():N}@test.dev", "password123", "Ada"));
        var first = (await registered.Content.ReadFromJsonAsync<AuthResponse>())!;

        var rotated = (await (await client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshRequest(first.RefreshToken))).Content.ReadFromJsonAsync<AuthResponse>())!;
        var replay = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(first.RefreshToken));
        var afterReplay = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(rotated.RefreshToken));

        Assert.NotEqual(first.RefreshToken, rotated.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterReplay.StatusCode);
    }

    [Fact]
    public async Task Profile_updates_and_rejects_unknown_avatars()
    {
        var client = await factory.CreateUserClientAsync();

        var bad = await client.PutAsJsonAsync("/api/me", new UpdateProfileRequest("Ada", null, "dragon", null));
        var good = await client.PutAsJsonAsync("/api/me",
            new UpdateProfileRequest(" Ada L. ", "Revising calculus", "owl", "dark"));
        var profile = await client.GetFromJsonAsync<ProfileResponse>("/api/me");

        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Equal(HttpStatusCode.OK, good.StatusCode);
        Assert.Equal("Ada L.", profile!.DisplayName);
        Assert.Equal("owl", profile.AvatarKey);
        Assert.Equal("dark", profile.ThemeKey);
    }

    [Fact]
    public async Task Protected_route_requires_a_token()
    {
        var response = await factory.CreateClient().GetAsync("/api/subjects");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

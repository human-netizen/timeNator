using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Tests;

public class LowLimitFactory : ApiFactory
{
    protected override int AuthPerMinute => 3;
    protected override int HubBurst => 3;
}

// Each class gets its own factory, and so its own limiter state; sharing one would make the
// outcome depend on which test ran first.
public class AuthRateLimitTests(LowLimitFactory factory) : IClassFixture<LowLimitFactory>
{
    [Fact]
    public async Task Login_attempts_beyond_the_window_are_refused()
    {
        var client = factory.CreateClient();
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 5; i++)
            statuses.Add((await client.PostAsJsonAsync("/api/auth/login",
                new LoginRequest("nobody@test.dev", "wrong-password"))).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, statuses[0]);
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[^1]);
    }
}

public class HubRateLimitTests(LowLimitFactory factory) : IClassFixture<LowLimitFactory>
{
    [Fact]
    public async Task Hub_calls_beyond_the_burst_are_refused()
    {
        var user = await factory.CreateUserAsync();
        await using var hub = await factory.ConnectHubAsync(user);

        // A burst of three is allowed, then calls are refused until the bucket refills.
        var failures = 0;
        for (var i = 0; i < 6; i++)
        {
            try
            {
                await hub.InvokeAsync("KeepPresence");
            }
            catch (HubException)
            {
                failures++;
            }
        }

        Assert.True(failures >= 2);
    }
}

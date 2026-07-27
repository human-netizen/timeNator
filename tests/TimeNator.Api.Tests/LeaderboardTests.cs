using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using TimeNator.Shared;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Tests;

[Collection(ApiCollection.Name)]
public class LeaderboardTests(ApiFactory factory)
{
    // A fixed past day, so these tests do not collide with sessions other tests save today.
    private static readonly DateTimeOffset Day = new(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Board_orders_by_seconds_and_survives_losing_the_cache()
    {
        var users = new[] { ("Ann", 1200), ("Ben", 3000), ("Cat", 600) };
        foreach (var (name, seconds) in users)
        {
            var user = await factory.CreateUserAsync(name);
            await SaveSessionAsync(user.Client, Day, seconds);
        }

        var board = await Get(Day);
        await factory.Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase()
            .KeyDeleteAsync("leaderboard:global:2026-07-01");
        var rebuilt = await Get(Day);

        var mine = board.Where(e => users.Any(u => u.Item1 == e.DisplayName)).ToList();
        Assert.Equal(["Ben", "Ann", "Cat"], mine.Select(e => e.DisplayName));
        Assert.Equal([3000, 1200, 600], mine.Select(e => e.DurationSeconds));
        Assert.Equal(board, rebuilt);
    }

    [Fact]
    public async Task A_second_session_adds_to_the_same_day()
    {
        var user = await factory.CreateUserAsync("Dee");
        await SaveSessionAsync(user.Client, Day.AddHours(1), 900);
        await Get(Day.AddHours(1));
        await SaveSessionAsync(user.Client, Day.AddHours(3), 300);

        var entry = (await Get(Day)).Single(e => e.UserId == user.Auth.UserId);

        Assert.Equal(1200, entry.DurationSeconds);
    }

    [Fact]
    public async Task Subscribers_get_the_new_board_when_today_changes()
    {
        var watcher = await factory.CreateUserAsync("Watcher");
        var studier = await factory.CreateUserAsync("Studier");
        var updated = new TaskCompletionSource<List<LeaderboardEntry>>();
        await using var hub = await factory.ConnectHubAsync(watcher);
        hub.On<List<LeaderboardEntry>>("LeaderboardUpdated", entries => updated.TrySetResult(entries));
        await hub.InvokeAsync("SubscribeLeaderboard");

        await SaveSessionAsync(studier.Client, DateTimeOffset.UtcNow.AddMinutes(-40), 1800);
        var entries = await updated.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Contains(entries, e => e.UserId == studier.Auth.UserId && e.DurationSeconds >= 1800);
    }

    private async Task<List<LeaderboardEntry>> Get(DateTimeOffset day)
    {
        var client = await factory.CreateUserClientAsync();
        return (await client.GetFromJsonAsync<List<LeaderboardEntry>>(
            $"/api/leaderboard?date={day:yyyy-MM-dd}"))!;
    }

    private static async Task SaveSessionAsync(HttpClient client, DateTimeOffset start, int seconds)
    {
        var subject = await (await client.PostAsJsonAsync("/api/subjects",
            new CreateSubjectRequest($"S{Guid.NewGuid():N}"[..20], "#00FF00"))).Content
            .ReadFromJsonAsync<SubjectResponse>();
        var response = await client.PostAsJsonAsync("/api/sessions", new CreateSessionRequest(subject!.Id, start,
            start.AddSeconds(seconds), seconds, 0, SessionMode.Stopwatch, SessionSource.Timer, seconds));
        response.EnsureSuccessStatusCode();
    }
}

using System.Net;
using System.Net.Http.Json;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Tests;

[Collection(ApiCollection.Name)]
public class PlannerTests(ApiFactory factory)
{
    // 2026-07-27 is a Monday.
    private static readonly DateOnly Monday = new(2026, 7, 27);

    [Fact]
    public async Task Repeating_todo_gets_an_independent_copy_on_each_day_it_applies()
    {
        var client = await factory.CreateUserClientAsync();
        await client.PostAsJsonAsync("/api/todos", new CreateTodoRequest("Flashcards", null, Monday, "weekdays"));

        var monday = await Day(client, Monday);
        var copy = Assert.Single(monday);
        await client.PutAsJsonAsync($"/api/todos/{copy.Id}", new UpdateTodoRequest(copy.Title, null, null, true));
        var mondayAgain = await Day(client, Monday);
        var tuesday = await Day(client, Monday.AddDays(1));
        var saturday = await Day(client, Monday.AddDays(5));

        Assert.NotNull(copy.RepeatParentId);
        Assert.True(Assert.Single(mondayAgain).IsDone);
        Assert.False(Assert.Single(tuesday).IsDone);
        Assert.Empty(saturday);
    }

    [Fact]
    public async Task One_off_todo_shows_on_its_day_and_undated_ones_until_done()
    {
        var client = await factory.CreateUserClientAsync();
        await client.PostAsJsonAsync("/api/todos", new CreateTodoRequest("Essay", null, Monday, null));
        var undated = await (await client.PostAsJsonAsync("/api/todos",
            new CreateTodoRequest("Buy pens", null, null, null))).Content.ReadFromJsonAsync<TodoResponse>();

        var monday = await Day(client, Monday);
        var tuesday = await Day(client, Monday.AddDays(1));
        await client.PutAsJsonAsync($"/api/todos/{undated!.Id}", new UpdateTodoRequest("Buy pens", null, null, true));
        var afterDone = await Day(client, Monday.AddDays(1));

        Assert.Equal(["Essay", "Buy pens"], monday.Select(t => t.Title));
        Assert.Equal(["Buy pens"], tuesday.Select(t => t.Title));
        Assert.Empty(afterDone);
    }

    [Fact]
    public async Task Bad_repeat_rules_are_rejected()
    {
        var client = await factory.CreateUserClientAsync();

        var noStart = await client.PostAsJsonAsync("/api/todos", new CreateTodoRequest("X", null, null, "daily"));
        var nonsense = await client.PostAsJsonAsync("/api/todos", new CreateTodoRequest("X", null, Monday, "hourly"));

        Assert.Equal(HttpStatusCode.BadRequest, noStart.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, nonsense.StatusCode);
    }

    private static async Task<List<TodoResponse>> Day(HttpClient client, DateOnly date) =>
        (await client.GetFromJsonAsync<List<TodoResponse>>($"/api/todos?date={date:yyyy-MM-dd}"))!;
}

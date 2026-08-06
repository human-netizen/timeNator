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

    [Fact]
    public async Task Timetable_orders_by_day_and_time_and_rejects_backwards_entries()
    {
        var client = await factory.CreateUserClientAsync();
        await client.PostAsJsonAsync("/api/timetable",
            new TimetableRequest(DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(10, 30), "Lab", null));
        await client.PostAsJsonAsync("/api/timetable",
            new TimetableRequest(DayOfWeek.Monday, new TimeOnly(14, 0), new TimeOnly(15, 0), "Lecture", null));

        var backwards = await client.PostAsJsonAsync("/api/timetable",
            new TimetableRequest(DayOfWeek.Monday, new TimeOnly(11, 0), new TimeOnly(10, 0), "Oops", null));
        var list = await client.GetFromJsonAsync<List<TimetableResponse>>("/api/timetable");

        Assert.Equal(HttpStatusCode.BadRequest, backwards.StatusCode);
        Assert.Equal(["Lecture", "Lab"], list!.Select(e => e.Title));
    }

    [Fact]
    public async Task Ddays_list_soonest_first()
    {
        var client = await factory.CreateUserClientAsync();
        await client.PostAsJsonAsync("/api/ddays", new DdayRequest("Finals", Monday.AddDays(60)));
        await client.PostAsJsonAsync("/api/ddays", new DdayRequest("Midterm", Monday.AddDays(20)));

        var list = await client.GetFromJsonAsync<List<DdayResponse>>("/api/ddays");

        Assert.Equal(["Midterm", "Finals"], list!.Select(d => d.Title));
    }

    [Fact]
    public async Task Daily_review_pairs_the_local_days_sessions_with_its_todos()
    {
        var client = await factory.CreateUserClientAsync();
        var subject = await (await client.PostAsJsonAsync("/api/subjects",
            new CreateSubjectRequest("Math", "#FF0000"))).Content.ReadFromJsonAsync<SubjectResponse>();
        await client.PostAsJsonAsync("/api/todos", new CreateTodoRequest("Chapter 3", subject!.Id, Monday, null));

        // In UTC+6, 23:00 Monday is 17:00 UTC and 00:30 Tuesday is 18:30 UTC, both Monday in UTC.
        // Only the first belongs to the local Monday.
        var offset = TimeSpan.FromHours(6);
        foreach (var (local, seconds) in new[] { (new DateTime(2026, 7, 27, 23, 0, 0), 1200),
                     (new DateTime(2026, 7, 28, 0, 30, 0), 600) })
        {
            var start = new DateTimeOffset(local, offset);
            await client.PostAsJsonAsync("/api/sessions", new CreateSessionRequest(subject.Id, start,
                start.AddSeconds(seconds), seconds, 0, Shared.SessionMode.Stopwatch, Shared.SessionSource.Timer,
                seconds));
        }

        var review = await client.GetFromJsonAsync<DailyReviewResponse>(
            $"/api/daily-review?date={Monday:yyyy-MM-dd}&offsetMinutes=360");

        Assert.Equal(1200, review!.TotalSeconds);
        Assert.Equal("Math", Assert.Single(review.Subjects).SubjectName);
        Assert.Equal("Chapter 3", Assert.Single(review.Todos).Title);
    }

    private static async Task<List<TodoResponse>> Day(HttpClient client, DateOnly date) =>
        (await client.GetFromJsonAsync<List<TodoResponse>>($"/api/todos?date={date:yyyy-MM-dd}"))!;
}

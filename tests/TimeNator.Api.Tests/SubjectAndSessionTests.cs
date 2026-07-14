using System.Net;
using System.Net.Http.Json;
using TimeNator.Shared;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Tests;

[Collection(ApiCollection.Name)]
public class SubjectAndSessionTests(ApiFactory factory)
{
    [Fact]
    public async Task Subjects_are_private_to_their_owner()
    {
        var alice = await factory.CreateUserClientAsync();
        var bob = await factory.CreateUserClientAsync();

        var created = await alice.PostAsJsonAsync("/api/subjects", new CreateSubjectRequest("Math", "#FF0000"));
        var subject = (await created.Content.ReadFromJsonAsync<SubjectResponse>())!;

        var bobsList = await bob.GetFromJsonAsync<List<SubjectResponse>>("/api/subjects");
        var bobDelete = await bob.DeleteAsync($"/api/subjects/{subject.Id}");

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Empty(bobsList!);
        Assert.Equal(HttpStatusCode.NotFound, bobDelete.StatusCode);
    }

    [Fact]
    public async Task Duplicate_subject_name_conflicts_until_archived()
    {
        var client = await factory.CreateUserClientAsync();
        var first = await client.PostAsJsonAsync("/api/subjects", new CreateSubjectRequest("Math", "#FF0000"));
        var subject = (await first.Content.ReadFromJsonAsync<SubjectResponse>())!;

        var duplicate = await client.PostAsJsonAsync("/api/subjects", new CreateSubjectRequest("Math", "#00FF00"));
        await client.DeleteAsync($"/api/subjects/{subject.Id}");
        var afterArchive = await client.PostAsJsonAsync("/api/subjects", new CreateSubjectRequest("Math", "#00FF00"));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.Created, afterArchive.StatusCode);
    }

    [Fact]
    public async Task Session_round_trips_and_overlap_is_rejected()
    {
        var client = await factory.CreateUserClientAsync();
        var subject = await CreateSubjectAsync(client);
        var start = DateTimeOffset.UtcNow.AddHours(-3);
        var request = Session(subject.Id, start, 1800, 120);

        var created = await client.PostAsJsonAsync("/api/sessions", request);
        var overlapping = await client.PostAsJsonAsync("/api/sessions",
            Session(subject.Id, start.AddMinutes(10), 600, 0));
        var list = await client.GetFromJsonAsync<List<SessionResponse>>(
            $"/api/sessions?from={Uri.EscapeDataString(start.AddHours(-1).ToString("O"))}" +
            $"&to={Uri.EscapeDataString(DateTimeOffset.UtcNow.ToString("O"))}");

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, overlapping.StatusCode);
        var saved = Assert.Single(list!);
        Assert.Equal(1800, saved.DurationSeconds);
        Assert.Equal(120, saved.PausedSeconds);
        Assert.Equal("Math", saved.SubjectName);
    }

    [Fact]
    public async Task Session_for_someone_elses_subject_is_rejected()
    {
        var alice = await factory.CreateUserClientAsync();
        var bob = await factory.CreateUserClientAsync();
        var alicesSubject = await CreateSubjectAsync(alice);

        var response = await bob.PostAsJsonAsync("/api/sessions",
            Session(alicesSubject.Id, DateTimeOffset.UtcNow.AddHours(-2), 600, 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<SubjectResponse> CreateSubjectAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/subjects", new CreateSubjectRequest("Math", "#FF0000"));
        return (await response.Content.ReadFromJsonAsync<SubjectResponse>())!;
    }

    private static CreateSessionRequest Session(Guid subjectId, DateTimeOffset start, int duration, int paused) =>
        new(subjectId, start, start.AddSeconds(duration + paused), duration, paused,
            SessionMode.Stopwatch, SessionSource.Timer, duration);
}

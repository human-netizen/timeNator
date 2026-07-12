using System.Net.Http.Json;
using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.Services;

public class ApiException(string message) : Exception(message);

public class ApiClient(HttpClient http) : IApiClient
{
    public Task<HealthReportResponse> GetHealthAsync(CancellationToken cancellationToken = default) =>
        GetAsync<HealthReportResponse>("health", cancellationToken);

    public Task<List<SubjectResponse>> GetSubjectsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<List<SubjectResponse>>("api/subjects", cancellationToken);

    public Task<SubjectResponse> CreateSubjectAsync(CreateSubjectRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<SubjectResponse>(HttpMethod.Post, "api/subjects", request, cancellationToken);

    public Task<SubjectResponse> UpdateSubjectAsync(Guid id, UpdateSubjectRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<SubjectResponse>(HttpMethod.Put, $"api/subjects/{id}", request, cancellationToken);

    public Task DeleteSubjectAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/subjects/{id}", null, cancellationToken);

    public Task<SessionResponse> CreateSessionAsync(CreateSessionRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<SessionResponse>(HttpMethod.Post, "api/sessions", request, cancellationToken);

    public Task<List<SessionResponse>> GetSessionsAsync(DateTimeOffset from, DateTimeOffset to,
        CancellationToken cancellationToken = default) =>
        GetAsync<List<SessionResponse>>(
            $"api/sessions?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}",
            cancellationToken);

    private Task<T> GetAsync<T>(string path, CancellationToken cancellationToken) =>
        SendAsync<T>(HttpMethod.Get, path, null, cancellationToken);

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body,
        CancellationToken cancellationToken)
    {
        using var response = await SendAsync(method, path, body, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
               ?? throw new ApiException("The server returned an empty response.");
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
            request.Content = JsonContent.Create(body, body.GetType());

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new ApiException("The server could not be reached.");
        }

        if (response.IsSuccessStatusCode)
            return response;

        using (response)
            throw new ApiException(await ProblemReader.ReadMessageAsync(response, cancellationToken));
    }
}

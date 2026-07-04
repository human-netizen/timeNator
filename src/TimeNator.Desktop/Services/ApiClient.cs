using System.Net.Http.Json;
using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.Services;

public class ApiClient(HttpClient http) : IApiClient
{
    public async Task<HealthReportResponse> GetHealthAsync(CancellationToken cancellationToken = default) =>
        await http.GetFromJsonAsync<HealthReportResponse>("health", cancellationToken)
        ?? throw new InvalidOperationException("Empty health response.");
}

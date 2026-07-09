using System.Net.Http.Json;
using System.Text.Json;

namespace TimeNator.Desktop.Services;

/// <summary>Turns an RFC 7807 problem response into one line for the UI.</summary>
public static class ProblemReader
{
    private record Problem(string? Title, Dictionary<string, string[]>? Errors);

    public static async Task<string> ReadMessageAsync(HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<Problem>(cancellationToken);
            var firstError = problem?.Errors?.Values.SelectMany(e => e).FirstOrDefault();
            return firstError ?? problem?.Title ?? $"Request failed ({(int)response.StatusCode}).";
        }
        catch (JsonException)
        {
            return $"Request failed ({(int)response.StatusCode}).";
        }
    }
}

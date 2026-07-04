using Microsoft.Extensions.Diagnostics.HealthChecks;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api;

public static class HealthResponse
{
    public static Task WriteAsync(HttpContext context, HealthReport report) =>
        context.Response.WriteAsJsonAsync(new HealthReportResponse(
            report.Status.ToString(),
            report.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString()),
            (int)report.TotalDuration.TotalMilliseconds));
}

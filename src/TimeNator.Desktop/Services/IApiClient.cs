using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.Services;

public interface IApiClient
{
    Task<HealthReportResponse> GetHealthAsync(CancellationToken cancellationToken = default);
}

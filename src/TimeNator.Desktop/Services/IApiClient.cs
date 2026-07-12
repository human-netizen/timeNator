using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.Services;

/// <summary>Authenticated calls to the API. Failures surface as <see cref="ApiException"/>.</summary>
public interface IApiClient
{
    Task<HealthReportResponse> GetHealthAsync(CancellationToken cancellationToken = default);

    Task<List<SubjectResponse>> GetSubjectsAsync(CancellationToken cancellationToken = default);
    Task<SubjectResponse> CreateSubjectAsync(CreateSubjectRequest request, CancellationToken cancellationToken = default);
    Task<SubjectResponse> UpdateSubjectAsync(Guid id, UpdateSubjectRequest request,
        CancellationToken cancellationToken = default);
    Task DeleteSubjectAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SessionResponse> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default);
    Task<List<SessionResponse>> GetSessionsAsync(DateTimeOffset from, DateTimeOffset to,
        CancellationToken cancellationToken = default);
}

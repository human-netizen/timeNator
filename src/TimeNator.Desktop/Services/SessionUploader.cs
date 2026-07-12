using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.Services;

public enum UploadOutcome
{
    Saved,
    Queued,
    Rejected
}

/// <summary>
/// Posts finished sessions. A session is journaled before the first attempt, so a
/// network failure or a crash cannot lose it; unreachable-server failures are retried
/// with exponential backoff until they succeed. A 4xx means the server will never
/// accept it, so it is dropped.
/// </summary>
public class SessionUploader(IApiClient api, SessionJournal journal)
{
    private static readonly TimeSpan FirstDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxDelay = TimeSpan.FromMinutes(5);
    private int _retrying;

    /// <summary>Raised on any successful upload; the flag is true when it came from the retry queue.</summary>
    public event Action<CreateSessionRequest, bool>? Uploaded;

    public async Task<(UploadOutcome Outcome, string? Error)> SubmitAsync(CreateSessionRequest request)
    {
        journal.AddPending(request);
        var (outcome, error) = await TryUploadAsync(request, fromRetry: false);
        if (outcome == UploadOutcome.Queued)
            StartRetrying();
        return (outcome, error);
    }

    /// <summary>Called on launch to push anything left over from a previous run.</summary>
    public void ResumePending()
    {
        if (journal.Read().Pending.Count > 0)
            StartRetrying();
    }

    private async Task<(UploadOutcome, string?)> TryUploadAsync(CreateSessionRequest request, bool fromRetry)
    {
        try
        {
            await api.CreateSessionAsync(request);
            journal.RemovePending(request);
            Uploaded?.Invoke(request, fromRetry);
            return (UploadOutcome.Saved, null);
        }
        catch (ApiException ex) when (ex.StatusCode is >= 400 and < 500)
        {
            journal.RemovePending(request);
            return (UploadOutcome.Rejected, ex.Message);
        }
        catch (ApiException ex)
        {
            return (UploadOutcome.Queued, ex.Message);
        }
    }

    private void StartRetrying()
    {
        if (Interlocked.Exchange(ref _retrying, 1) == 1)
            return;
        _ = Task.Run(RetryLoopAsync);
    }

    private async Task RetryLoopAsync()
    {
        var delay = FirstDelay;
        try
        {
            while (journal.Read().Pending is { Count: > 0 } pending)
            {
                await Task.Delay(delay);
                var allSaved = true;
                foreach (var request in pending)
                {
                    var (outcome, _) = await TryUploadAsync(request, fromRetry: true);
                    allSaved &= outcome != UploadOutcome.Queued;
                }
                delay = allSaved ? FirstDelay : TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, MaxDelay.Ticks));
            }
        }
        finally
        {
            Interlocked.Exchange(ref _retrying, 0);
        }
    }
}

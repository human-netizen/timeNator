using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeNator.Desktop.Services;
using TimeNator.Shared;
using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.ViewModels;

/// <summary>
/// Adds a session that was not timed: forgotten time (Manual) or study away from the
/// computer (Offline). Both reuse the ordinary session POST and its sanity rules.
/// </summary>
public partial class ManualEntryViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly TimeProvider _clock;

    public ManualEntryViewModel(IApiClient api, SubjectCatalog catalog, TimeProvider clock)
    {
        _api = api;
        _clock = clock;
        Subjects = catalog.Subjects;
        var now = clock.GetLocalNow();
        Date = now.Date;
        StartTime = TimeSpan.FromHours(Math.Max(0, now.Hour - 1));
    }

    public event Action? Saved;

    public ObservableCollection<SubjectResponse> Subjects { get; }

    [ObservableProperty] public partial SubjectResponse? Subject { get; set; }
    [ObservableProperty] public partial DateTime? Date { get; set; }
    [ObservableProperty] public partial TimeSpan? StartTime { get; set; }
    [ObservableProperty] public partial decimal? Minutes { get; set; } = 30;
    [ObservableProperty] public partial bool IsOffline { get; set; }
    [ObservableProperty] public partial string? Message { get; set; }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (Subject is null || Date is null || StartTime is null || Minutes is not > 0)
        {
            Message = "Choose a subject, a date, a start time and a length.";
            return;
        }

        var localStart = Date.Value.Date + StartTime.Value;
        var start = new DateTimeOffset(localStart, _clock.LocalTimeZone.GetUtcOffset(localStart));
        var seconds = (int)(Minutes.Value * 60);
        var request = new CreateSessionRequest(Subject.Id, start, start.AddSeconds(seconds), seconds, 0,
            SessionMode.Stopwatch, IsOffline ? SessionSource.Offline : SessionSource.Manual, seconds);

        try
        {
            await _api.CreateSessionAsync(request);
            Message = $"Added {Minutes:0} min of {Subject.Name}.";
            Saved?.Invoke();
        }
        catch (ApiException ex)
        {
            Message = ex.Message;
        }
    }
}

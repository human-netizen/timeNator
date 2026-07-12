using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeNator.Desktop.Services;
using TimeNator.Shared;
using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.ViewModels;

public record HistoryItem(string SubjectName, string ColorHex, string TimeRange, string Duration, string Note);

public record HistoryDay(string Title, string Total, IReadOnlyList<HistoryItem> Sessions);

public partial class HistoryViewModel : ViewModelBase
{
    private const int PageDays = 30;

    private readonly IApiClient _api;
    private readonly TimeProvider _clock;
    private int _daysLoaded = PageDays;

    public HistoryViewModel(IApiClient api, SessionUploader uploader, TimeProvider clock)
    {
        _api = api;
        _clock = clock;
        uploader.Uploaded += (_, _) => Dispatcher.UIThread.Post(() => LoadCommand.Execute(null));
    }

    public ObservableCollection<HistoryDay> Days { get; } = [];

    [ObservableProperty] public partial string? Error { get; set; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        // Day boundaries are local midnight; the API only ever sees UTC instants.
        var today = _clock.GetLocalNow().Date;
        var to = new DateTimeOffset(today.AddDays(1), _clock.LocalTimeZone.GetUtcOffset(today.AddDays(1)));
        var fromDate = today.AddDays(1 - _daysLoaded);
        var from = new DateTimeOffset(fromDate, _clock.LocalTimeZone.GetUtcOffset(fromDate));

        try
        {
            var sessions = await _api.GetSessionsAsync(from, to);
            Days.Clear();
            foreach (var day in Group(sessions, today))
                Days.Add(day);
            Error = null;
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
    }

    [RelayCommand]
    private Task LoadOlderAsync()
    {
        _daysLoaded += PageDays;
        return LoadAsync();
    }

    private IEnumerable<HistoryDay> Group(IEnumerable<SessionResponse> sessions, DateTime today) =>
        sessions
            .Select(s => (Session: s, Start: TimeZoneInfo.ConvertTime(s.StartedAt, _clock.LocalTimeZone)))
            .GroupBy(x => x.Start.Date)
            .OrderByDescending(g => g.Key)
            .Select(g => new HistoryDay(
                DayTitle(g.Key, today),
                FormatDuration(g.Sum(x => x.Session.DurationSeconds)),
                g.OrderBy(x => x.Start).Select(x => ToItem(x.Session, x.Start)).ToList()));

    private HistoryItem ToItem(SessionResponse s, DateTimeOffset localStart)
    {
        var localEnd = TimeZoneInfo.ConvertTime(s.EndedAt, _clock.LocalTimeZone);
        var note = s.Source == SessionSource.Timer ? s.Mode.ToString() : s.Source.ToString();
        return new HistoryItem(s.SubjectName, s.SubjectColorHex, $"{localStart:HH:mm} – {localEnd:HH:mm}",
            FormatDuration(s.DurationSeconds), note);
    }

    private static string DayTitle(DateTime day, DateTime today) =>
        day == today ? "Today" : day == today.AddDays(-1) ? "Yesterday" : day.ToString("dddd, d MMMM");

    private static string FormatDuration(int seconds)
    {
        var span = TimeSpan.FromSeconds(seconds);
        return span.TotalHours >= 1 ? $"{(int)span.TotalHours}h {span.Minutes:00}m" : $"{span.Minutes}m {span.Seconds:00}s";
    }
}

using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeNator.Desktop.Services;
using TimeNator.Shared;
using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.ViewModels;

public record HistoryItem(string SubjectName, string ColorHex, string TimeRange, string Duration, string Note);

public record HistoryDay(string Title, string Total, IReadOnlyList<HistoryItem> Sessions, string? DayOffLabel)
{
    public bool IsDayOff => DayOffLabel is not null;
}

public partial class HistoryViewModel : ViewModelBase
{
    private const int PageDays = 30;

    private readonly IApiClient _api;
    private readonly TimeProvider _clock;
    private int _daysLoaded = PageDays;

    public HistoryViewModel(IApiClient api, SessionUploader uploader, ManualEntryViewModel manualEntry,
        TimeProvider clock)
    {
        _api = api;
        _clock = clock;
        uploader.Uploaded += (_, _) => Dispatcher.UIThread.Post(() => LoadCommand.Execute(null));
        ManualEntry = manualEntry;
        ManualEntry.Saved += () => LoadCommand.Execute(null);
    }

    public ManualEntryViewModel ManualEntry { get; }

    public ObservableCollection<HistoryDay> Days { get; } = [];

    [ObservableProperty] public partial string? Error { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DayOffButtonText))]
    public partial bool IsTodayOff { get; set; }

    public string DayOffButtonText => IsTodayOff ? "Undo day off" : "Take today off";

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
            var dayOffs = await _api.GetDayOffsAsync(DateOnly.FromDateTime(fromDate), DateOnly.FromDateTime(today));
            IsTodayOff = dayOffs.Any(d => d.Date == DateOnly.FromDateTime(today));
            Days.Clear();
            foreach (var day in Group(sessions, dayOffs, today))
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

    [RelayCommand]
    private async Task ToggleTodayOffAsync()
    {
        var today = DateOnly.FromDateTime(_clock.GetLocalNow().Date);
        try
        {
            if (IsTodayOff)
                await _api.DeleteDayOffAsync(today);
            else
                await _api.CreateDayOffAsync(new DayOffRequest(today, null));
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
    }

    private IEnumerable<HistoryDay> Group(
        IEnumerable<SessionResponse> sessions, IEnumerable<DayOffResponse> dayOffs, DateTime today)
    {
        var byDay = sessions
            .Select(s => (Session: s, Start: TimeZoneInfo.ConvertTime(s.StartedAt, _clock.LocalTimeZone)))
            .ToLookup(x => x.Start.Date);
        var offs = dayOffs.ToDictionary(d => d.Date.ToDateTime(TimeOnly.MinValue));

        return byDay.Select(g => g.Key).Union(offs.Keys)
            .OrderByDescending(day => day)
            .Select(day => new HistoryDay(
                DayTitle(day, today),
                DayTotal(byDay[day].Select(x => x.Session)),
                byDay[day].OrderBy(x => x.Start).Select(x => ToItem(x.Session, x.Start)).ToList(),
                offs.TryGetValue(day, out var off) ? off.Note ?? "Day off" : null));
    }

    private HistoryItem ToItem(SessionResponse s, DateTimeOffset localStart)
    {
        var localEnd = TimeZoneInfo.ConvertTime(s.EndedAt, _clock.LocalTimeZone);
        var note = s.Source == SessionSource.Timer ? s.Mode.ToString() : s.Source.ToString();
        return new HistoryItem(s.SubjectName, s.SubjectColorHex, $"{localStart:HH:mm} – {localEnd:HH:mm}",
            FormatDuration(s.DurationSeconds), note);
    }

    // Offline time is study away from the computer; it counts, but is shown apart.
    private static string DayTotal(IEnumerable<SessionResponse> sessions)
    {
        var list = sessions.ToList();
        var total = FormatDuration(list.Sum(s => s.DurationSeconds));
        var offline = list.Where(s => s.Source == SessionSource.Offline).Sum(s => s.DurationSeconds);
        return offline > 0 ? $"{total} ({FormatDuration(offline)} offline)" : total;
    }

    private static string DayTitle(DateTime day, DateTime today) =>
        day == today ? "Today" : day == today.AddDays(-1) ? "Yesterday" : day.ToString("dddd, d MMMM");

    private static string FormatDuration(int seconds)
    {
        var span = TimeSpan.FromSeconds(seconds);
        return span.TotalHours >= 1 ? $"{(int)span.TotalHours}h {span.Minutes:00}m" : $"{span.Minutes}m {span.Seconds:00}s";
    }
}

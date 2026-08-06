using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeNator.Desktop.Services;
using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.ViewModels;

public record TimetableSlot(Guid Id, string Time, string Title, string ColorHex);

public record TimetableDay(string Name, ObservableCollection<TimetableSlot> Slots);

/// <summary>The week's fixed commitments as seven columns, Monday first.</summary>
public partial class TimetableViewModel(IApiClient api, SubjectCatalog catalog) : ViewModelBase
{
    private static readonly DayOfWeek[] WeekOrder =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday,
        DayOfWeek.Saturday, DayOfWeek.Sunday
    ];

    public static IReadOnlyList<DayOfWeek> DayChoices => WeekOrder;

    public ObservableCollection<TimetableDay> Days { get; } =
        new(WeekOrder.Select(d => new TimetableDay(d.ToString()[..3], [])));

    public ObservableCollection<SubjectResponse> Subjects => catalog.Subjects;

    [ObservableProperty] public partial DayOfWeek NewDay { get; set; } = DayOfWeek.Monday;
    [ObservableProperty] public partial TimeSpan? NewStart { get; set; } = TimeSpan.FromHours(9);
    [ObservableProperty] public partial TimeSpan? NewEnd { get; set; } = TimeSpan.FromHours(10);
    [ObservableProperty] public partial string NewTitle { get; set; } = "";
    [ObservableProperty] public partial SubjectResponse? NewSubject { get; set; }
    [ObservableProperty] public partial string? Error { get; private set; }

    public override Task ActivateAsync() => LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            var entries = await api.GetTimetableAsync();
            foreach (var day in Days)
                day.Slots.Clear();
            foreach (var e in entries)
                Days[Array.IndexOf(WeekOrder, e.DayOfWeek)].Slots.Add(new TimetableSlot(e.Id,
                    $"{e.StartTime:HH:mm}–{e.EndTime:HH:mm}", e.Title, e.SubjectColorHex ?? "#95A5A6"));
            Error = null;
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (NewStart is not { } start || NewEnd is not { } end || string.IsNullOrWhiteSpace(NewTitle))
        {
            Error = "Give the entry a title, a start and an end.";
            return;
        }
        try
        {
            await api.CreateTimetableEntryAsync(new TimetableRequest(NewDay, TimeOnly.FromTimeSpan(start),
                TimeOnly.FromTimeSpan(end), NewTitle.Trim(), NewSubject?.Id));
            NewTitle = "";
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(TimetableSlot slot)
    {
        try
        {
            await api.DeleteTimetableEntryAsync(slot.Id);
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
    }
}

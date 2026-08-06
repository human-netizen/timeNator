using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeNator.Desktop.Services;
using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.ViewModels;

public record RepeatOption(string Name, string? Rule);

public record DdayRow(Guid Id, string Title, string Countdown, string Date);

public record SubjectRow(string Name, string ColorHex, string Duration);

public partial class TodoRow(TodoResponse todo) : ObservableObject
{
    public TodoResponse Todo { get; } = todo;
    public string Title => Todo.Title;
    public string? Detail => Todo.SubjectName ?? (Todo.RepeatParentId is not null ? "repeating" : null);
    public bool IsDone => Todo.IsDone;
}

/// <summary>
/// The planner's day view: pick a day, see what was studied (the daily review), tick off
/// the day's to-dos, and keep an eye on D-day countdowns.
/// </summary>
public partial class PlannerViewModel(IApiClient api, SubjectCatalog catalog, TimeProvider clock) : ViewModelBase
{
    public static IReadOnlyList<RepeatOption> RepeatOptions { get; } =
    [
        new("Once", null),
        new("Every day", "daily"),
        new("Weekdays", "weekdays")
    ];

    public ObservableCollection<SubjectResponse> Subjects => catalog.Subjects;
    public ObservableCollection<TodoRow> Todos { get; } = [];
    public ObservableCollection<SubjectRow> StudiedSubjects { get; } = [];
    public ObservableCollection<DdayRow> Ddays { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DayTitle))]
    public partial DateOnly Day { get; private set; } = DateOnly.FromDateTime(clock.GetLocalNow().Date);

    [ObservableProperty] public partial string StudiedText { get; private set; } = "";
    [ObservableProperty] public partial string TodoProgress { get; private set; } = "";
    [ObservableProperty] public partial string? Error { get; private set; }

    [ObservableProperty] public partial string NewTodo { get; set; } = "";
    [ObservableProperty] public partial SubjectResponse? NewTodoSubject { get; set; }
    [ObservableProperty] public partial RepeatOption NewTodoRepeat { get; set; } = RepeatOptions[0];

    [ObservableProperty] public partial string NewDdayTitle { get; set; } = "";
    [ObservableProperty] public partial DateTime? NewDdayDate { get; set; }

    private DateOnly Today => DateOnly.FromDateTime(clock.GetLocalNow().Date);

    public string DayTitle => Day == Today ? "Today"
        : Day == Today.AddDays(-1) ? "Yesterday"
        : Day == Today.AddDays(1) ? "Tomorrow"
        : Day.ToString("dddd, d MMMM");

    public override Task ActivateAsync() => LoadAsync();

    [RelayCommand]
    private Task PreviousDayAsync() => MoveAsync(-1);

    [RelayCommand]
    private Task NextDayAsync() => MoveAsync(1);

    [RelayCommand]
    private Task TodayAsync()
    {
        Day = Today;
        return LoadAsync();
    }

    [RelayCommand]
    private Task LoadAsync() => RunAsync(async () =>
    {
        var offset = clock.LocalTimeZone.GetUtcOffset(Day.ToDateTime(TimeOnly.MinValue));
        var review = await api.GetDailyReviewAsync(Day, offset);

        StudiedSubjects.Clear();
        foreach (var s in review.Subjects)
            StudiedSubjects.Add(new SubjectRow(s.SubjectName, s.ColorHex, Format(s.Seconds)));
        StudiedText = review.SessionCount == 0
            ? "Nothing studied."
            : $"{Format(review.TotalSeconds)} studied in {review.SessionCount} sessions";

        ShowTodos(review.Todos);
        await LoadDdaysAsync();
    });

    [RelayCommand]
    private Task AddTodoAsync() => RunAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(NewTodo))
            return;
        await api.CreateTodoAsync(new CreateTodoRequest(NewTodo.Trim(), NewTodoSubject?.Id, Day, NewTodoRepeat.Rule));
        NewTodo = "";
        NewTodoRepeat = RepeatOptions[0];
        await LoadAsync();
    });

    [RelayCommand]
    private Task ToggleTodoAsync(TodoRow row) => RunAsync(async () =>
    {
        var t = row.Todo;
        await api.UpdateTodoAsync(t.Id, new UpdateTodoRequest(t.Title, t.SubjectId, t.DueDate, !t.IsDone));
        await LoadAsync();
    });

    [RelayCommand]
    private Task DeleteTodoAsync(TodoRow row) => RunAsync(async () =>
    {
        // Deleting a day's copy of a repeating to-do removes the whole series.
        await api.DeleteTodoAsync(row.Todo.RepeatParentId ?? row.Todo.Id);
        await LoadAsync();
    });

    [RelayCommand]
    private Task AddDdayAsync() => RunAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(NewDdayTitle) || NewDdayDate is not { } date)
            return;
        await api.CreateDdayAsync(new DdayRequest(NewDdayTitle.Trim(), DateOnly.FromDateTime(date)));
        NewDdayTitle = "";
        NewDdayDate = null;
        await LoadDdaysAsync();
    });

    [RelayCommand]
    private Task DeleteDdayAsync(DdayRow row) => RunAsync(async () =>
    {
        await api.DeleteDdayAsync(row.Id);
        await LoadDdaysAsync();
    });

    private async Task MoveAsync(int days)
    {
        Day = Day.AddDays(days);
        await LoadAsync();
    }

    private void ShowTodos(List<TodoResponse> todos)
    {
        Todos.Clear();
        foreach (var todo in todos)
            Todos.Add(new TodoRow(todo));
        TodoProgress = todos.Count == 0 ? "No to-dos." : $"{todos.Count(t => t.IsDone)} of {todos.Count} done";
    }

    private async Task LoadDdaysAsync()
    {
        var ddays = await api.GetDdaysAsync();
        Ddays.Clear();
        foreach (var d in ddays)
        {
            var days = d.TargetDate.DayNumber - Today.DayNumber;
            var countdown = days == 0 ? "D-Day" : days > 0 ? $"D-{days}" : $"D+{-days}";
            Ddays.Add(new DdayRow(d.Id, d.Title, countdown, d.TargetDate.ToString("d MMM yyyy")));
        }
    }

    private async Task RunAsync(Func<Task> action)
    {
        try
        {
            Error = null;
            await action();
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
    }

    private static string Format(int seconds)
    {
        var span = TimeSpan.FromSeconds(seconds);
        return span.TotalHours >= 1 ? $"{(int)span.TotalHours}h {span.Minutes:00}m" : $"{span.Minutes}m";
    }
}

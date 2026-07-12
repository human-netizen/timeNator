using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeNator.Desktop.Models;
using TimeNator.Desktop.Services;
using TimeNator.Shared;
using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.ViewModels;

public partial class TimerViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly StudyTimer _timer;
    private readonly DispatcherTimer _tick;

    public TimerViewModel(IApiClient api, SubjectCatalog catalog, TimeProvider clock)
    {
        _api = api;
        _timer = new StudyTimer(clock);
        Subjects = catalog.Subjects;
        _tick = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Normal, (_, _) => Refresh());
    }

    public ObservableCollection<SubjectResponse> Subjects { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    public partial SubjectResponse? SelectedSubject { get; set; }

    [ObservableProperty] public partial string ElapsedText { get; set; } = "00:00:00";
    [ObservableProperty] public partial string? Message { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle), nameof(IsRunning), nameof(IsPaused))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(StopCommand))]
    public partial TimerState State { get; private set; }

    public bool IsIdle => State == TimerState.Idle;
    public bool IsRunning => State == TimerState.Running;
    public bool IsPaused => State == TimerState.Paused;

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start()
    {
        _timer.Start();
        _tick.Start();
        Message = null;
        Refresh();
    }

    private bool CanStart() => State == TimerState.Idle && SelectedSubject is not null;

    [RelayCommand]
    private void Pause()
    {
        _timer.Pause();
        Refresh();
    }

    [RelayCommand]
    private void Resume()
    {
        _timer.Resume();
        Refresh();
    }

    [RelayCommand(CanExecute = nameof(CanStop))]
    private async Task StopAsync()
    {
        var subject = SelectedSubject!;
        var timing = _timer.Stop();
        _tick.Stop();
        Refresh();

        if (timing.DurationSeconds < 1)
        {
            Message = "Session too short to save.";
            return;
        }

        try
        {
            await _api.CreateSessionAsync(new CreateSessionRequest(
                subject.Id, timing.StartedAt, timing.EndedAt, timing.DurationSeconds, timing.PausedSeconds,
                SessionMode.Stopwatch, SessionSource.Timer, timing.DurationSeconds));
            Message = $"Saved {Format(TimeSpan.FromSeconds(timing.DurationSeconds))} of {subject.Name}.";
        }
        catch (ApiException ex)
        {
            Message = ex.Message;
        }
    }

    private bool CanStop() => State != TimerState.Idle;

    private void Refresh()
    {
        State = _timer.State;
        ElapsedText = Format(_timer.Elapsed);
    }

    private static string Format(TimeSpan span) => $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}";
}

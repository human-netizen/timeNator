using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeNator.Desktop.Interop;
using TimeNator.Desktop.Models;
using TimeNator.Desktop.Services;
using TimeNator.Shared;
using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.ViewModels;

public partial class TimerViewModel : ViewModelBase
{
    private static readonly TimeSpan JournalInterval = TimeSpan.FromSeconds(30);

    private readonly SessionJournal _journal;
    private readonly SessionUploader _uploader;
    private readonly IIdleDetector _idle;
    private readonly SettingsStore _settings;
    private readonly IWindowService _windows;
    private readonly TimeProvider _clock;
    private readonly StudyTimer _timer;
    private readonly DispatcherTimer _tick;
    private DateTimeOffset _lastJournaled;
    private SessionMode _runningMode;
    private PomodoroCycle? _pomodoro;

    public TimerViewModel(SubjectCatalog catalog, SessionJournal journal, SessionUploader uploader,
        IIdleDetector idle, SettingsStore settings, IWindowService windows, TimeProvider clock)
    {
        _journal = journal;
        _uploader = uploader;
        _clock = clock;
        _idle = idle;
        _settings = settings;
        _windows = windows;
        _timer = new StudyTimer(clock);
        Subjects = catalog.Subjects;
        _tick = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _tick.Tick += (_, _) => OnTick();
        _uploader.Uploaded += (request, fromRetry) =>
        {
            if (fromRetry)
                Dispatcher.UIThread.Post(() => Message =
                    $"Uploaded a queued session of {Format(TimeSpan.FromSeconds(request.DurationSeconds))}.");
        };
    }

    public ObservableCollection<SubjectResponse> Subjects { get; }

    public static IReadOnlyList<SessionMode> Modes { get; } =
        [SessionMode.Stopwatch, SessionMode.Countdown, SessionMode.Pomodoro];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    public partial SubjectResponse? SelectedSubject { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCountdown))]
    public partial SessionMode SelectedMode { get; set; } = SessionMode.Stopwatch;

    public bool IsCountdown => SelectedMode == SessionMode.Countdown;

    [ObservableProperty] public partial decimal? CountdownMinutes { get; set; } = 50;

    [ObservableProperty] public partial string ClockText { get; set; } = "00:00:00";
    [ObservableProperty] public partial string DetailText { get; set; } = "";
    [ObservableProperty] public partial string? Message { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRecovered))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    public partial ActiveSession? Recovered { get; private set; }

    public bool HasRecovered => Recovered is not null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle), nameof(IsRunning), nameof(IsPaused))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(StopCommand))]
    public partial TimerState State { get; private set; }

    public bool IsIdle => State == TimerState.Idle;
    public bool IsRunning => State == TimerState.Running;
    public bool IsPaused => State == TimerState.Paused;

    private TimeSpan CountdownTarget => TimeSpan.FromMinutes((double)(CountdownMinutes ?? 0));

    public override Task ActivateAsync()
    {
        _uploader.ResumePending();
        var active = _journal.Read().Active;
        if (active is not null)
        {
            Recovered = active;
            var timing = active.Timer.FinishAt(active.LastSeenAt);
            Message = $"An unfinished {active.SubjectName} session of " +
                      $"{Format(TimeSpan.FromSeconds(timing.DurationSeconds))} was recovered.";
        }
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SaveRecoveredAsync()
    {
        if (Recovered is not { } active)
            return;
        Recovered = null;
        var timing = active.Timer.FinishAt(active.LastSeenAt);
        await SubmitAsync(active.SubjectId, active.SubjectName, active.Mode, timing);
    }

    [RelayCommand]
    private void DiscardRecovered()
    {
        Recovered = null;
        _journal.SetActive(null);
        Message = "Recovered session discarded.";
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start()
    {
        if (SelectedMode == SessionMode.Countdown && CountdownTarget <= TimeSpan.Zero)
        {
            Message = "Set a countdown length first.";
            return;
        }

        _runningMode = SelectedMode;
        var settings = _settings.Current;
        _pomodoro = SelectedMode == SessionMode.Pomodoro
            ? new PomodoroCycle(TimeSpan.FromMinutes(settings.PomodoroFocusMinutes),
                TimeSpan.FromMinutes(settings.PomodoroBreakMinutes))
            : null;
        _timer.Start();
        _tick.Start();
        Message = null;
        Journal();
        Refresh();
    }

    private bool CanStart() => State == TimerState.Idle && SelectedSubject is not null && Recovered is null;

    [RelayCommand]
    private void Pause()
    {
        _timer.Pause();
        Journal();
        Refresh();
    }

    [RelayCommand]
    private void Resume()
    {
        // Resuming by hand during a pomodoro break skips the rest of the break.
        if (_pomodoro?.Phase == PomodoroPhase.Break)
            _pomodoro.StartFocus(_timer.Elapsed);
        _timer.Resume();
        Journal();
        Refresh();
    }

    [RelayCommand(CanExecute = nameof(CanStop))]
    private async Task StopAsync()
    {
        var subject = SelectedSubject!;
        var timing = _timer.Stop();
        _tick.Stop();
        Refresh();
        await SubmitAsync(subject.Id, subject.Name, _runningMode, timing);
    }

    private bool CanStop() => State != TimerState.Idle;

    [RelayCommand]
    private void OpenDeskMode() => _windows.ShowDeskMode(this);

    private async Task SubmitAsync(Guid subjectId, string subjectName, SessionMode mode, CompletedTiming timing)
    {
        if (timing.DurationSeconds < 1)
        {
            _journal.SetActive(null);
            Message = "Session too short to save.";
            return;
        }

        var request = new CreateSessionRequest(subjectId, timing.StartedAt, timing.EndedAt,
            timing.DurationSeconds, timing.PausedSeconds, mode, SessionSource.Timer, timing.MaxStreakSeconds);

        // SubmitAsync journals the finished session before its first await, so clearing
        // the active entry afterwards never leaves a window where the session is on disk nowhere.
        var submit = _uploader.SubmitAsync(request);
        _journal.SetActive(null);
        var (outcome, error) = await submit;

        var length = Format(TimeSpan.FromSeconds(timing.DurationSeconds));
        Message = outcome switch
        {
            UploadOutcome.Saved => $"Saved {length} of {subjectName}.",
            UploadOutcome.Queued => $"Server unreachable. {length} of {subjectName} will upload when it is back.",
            _ => $"The session was rejected: {error}"
        };
    }

    private void OnTick()
    {
        var idle = _idle.GetIdleTime();
        if (_timer.State == TimerState.Running && idle >= TimeSpan.FromMinutes(_settings.Current.IdleThresholdMinutes))
        {
            // Backdate the pause to the last input, so the idle minutes never count as study.
            _timer.Pause(_clock.GetUtcNow() - idle);
            Journal();
            Message = $"Paused after {(int)idle.TotalMinutes} min without input.";
        }

        switch (_pomodoro?.Update(_timer.Elapsed, _clock.GetUtcNow()))
        {
            case PomodoroPhase.Break:
                _timer.Pause();
                Journal();
                Message = $"Focus block {_pomodoro.CompletedFocusBlocks} done. Take a break.";
                break;
            case PomodoroPhase.Focus:
                _timer.Resume();
                Journal();
                Message = "Break over. Back to focus.";
                break;
        }

        if (_runningMode == SessionMode.Countdown && _timer.Elapsed >= CountdownTarget)
        {
            _ = FinishCountdownAsync();
            return;
        }

        if (_clock.GetUtcNow() - _lastJournaled >= JournalInterval)
            Journal();
        Refresh();
    }

    private async Task FinishCountdownAsync()
    {
        await StopAsync();
        Message = $"Countdown finished. {Message}";
    }

    private void Journal()
    {
        _lastJournaled = _clock.GetUtcNow();
        _journal.SetActive(new ActiveSession(SelectedSubject!.Id, SelectedSubject.Name, _runningMode,
            _timer.Snapshot(), _lastJournaled));
    }

    private void Refresh()
    {
        State = _timer.State;
        if (State == TimerState.Idle)
        {
            ClockText = SelectedMode switch
            {
                SessionMode.Countdown => Format(CountdownTarget),
                SessionMode.Pomodoro => Format(TimeSpan.FromMinutes(_settings.Current.PomodoroFocusMinutes)),
                _ => Format(TimeSpan.Zero)
            };
            DetailText = "";
            return;
        }

        var streak = $"streak {Format(_timer.CurrentStreak)}";
        if (_pomodoro is not null)
        {
            ClockText = Format(_pomodoro.Remaining(_timer.Elapsed, _clock.GetUtcNow()));
            var phase = _pomodoro.Phase == PomodoroPhase.Focus
                ? $"Focus {_pomodoro.CompletedFocusBlocks + 1}"
                : "Break";
            DetailText = $"{phase} · studied {Format(_timer.Elapsed)}";
        }
        else if (_runningMode == SessionMode.Countdown)
        {
            var remaining = CountdownTarget - _timer.Elapsed;
            ClockText = Format(remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining);
            DetailText = $"Studied {Format(_timer.Elapsed)} · {streak}";
        }
        else
        {
            ClockText = Format(_timer.Elapsed);
            DetailText = $"Current {streak}";
        }
    }

    partial void OnSelectedModeChanged(SessionMode value) => Refresh();

    partial void OnCountdownMinutesChanged(decimal? value) => Refresh();

    private static string Format(TimeSpan span) => $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}";
}

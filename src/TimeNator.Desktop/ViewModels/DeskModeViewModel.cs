using System.ComponentModel;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TimeNator.Desktop.ViewModels;

/// <summary>
/// A fullscreen, nearly black clock for leaving on a desk. The text drifts to a new
/// spot every minute so no pixel shows the same thing for hours (burn-in on OLED).
/// </summary>
public partial class DeskModeViewModel : ViewModelBase, IDisposable
{
    private static readonly TimeSpan ShiftInterval = TimeSpan.FromMinutes(1);
    private const double MaxShift = 120;

    private readonly TimerViewModel _timer;
    private readonly DispatcherTimer _shift;

    public DeskModeViewModel(TimerViewModel timer)
    {
        _timer = timer;
        _timer.PropertyChanged += OnTimerChanged;
        _shift = new DispatcherTimer { Interval = ShiftInterval };
        _shift.Tick += (_, _) => Offset = new Thickness(
            Random.Shared.NextDouble() * MaxShift * 2 - MaxShift, Random.Shared.NextDouble() * MaxShift * 2 - MaxShift,
            0, 0);
        _shift.Start();
    }

    public event Action? CloseRequested;

    public string ClockText => _timer.ClockText;
    public string DetailText => _timer.DetailText;
    public string SubjectName => _timer.SelectedSubject?.Name ?? "";

    [ObservableProperty] public partial Thickness Offset { get; private set; }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();

    private void OnTimerChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TimerViewModel.ClockText) or nameof(TimerViewModel.DetailText))
            OnPropertyChanged(e.PropertyName);
    }

    public void Dispose()
    {
        _shift.Stop();
        _timer.PropertyChanged -= OnTimerChanged;
    }
}

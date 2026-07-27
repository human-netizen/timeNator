using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeNator.Desktop.Services;
using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.ViewModels;

public record LeaderboardRow(int Rank, string DisplayName, string Duration, bool IsMe);

/// <summary>
/// Today's global top 50. Subscribes to live updates only while visible, so a busy
/// board does not wake every connected client on every saved session.
/// </summary>
public partial class LeaderboardViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly IStudyHubClient _hub;
    private readonly IAuthService _auth;
    private bool _open;

    public LeaderboardViewModel(IApiClient api, IStudyHubClient hub, IAuthService auth)
    {
        _api = api;
        _hub = hub;
        _auth = auth;
        _hub.LeaderboardUpdated += entries => Dispatcher.UIThread.Post(() =>
        {
            if (_open)
                Show(entries);
        });
    }

    public ObservableCollection<LeaderboardRow> Rows { get; } = [];

    [ObservableProperty] public partial string? Error { get; private set; }

    public async Task OpenAsync()
    {
        if (_open)
            return;
        _open = true;
        await _hub.SubscribeLeaderboardAsync();
        await RefreshAsync();
    }

    public async Task CloseAsync()
    {
        if (!_open)
            return;
        _open = false;
        await _hub.UnsubscribeLeaderboardAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            Show(await _api.GetLeaderboardAsync());
            Error = null;
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
    }

    private void Show(List<LeaderboardEntry> entries)
    {
        Rows.Clear();
        foreach (var e in entries)
        {
            var span = TimeSpan.FromSeconds(e.DurationSeconds);
            Rows.Add(new LeaderboardRow(e.Rank, e.DisplayName, $"{(int)span.TotalHours}h {span.Minutes:00}m",
                e.UserId == _auth.UserId));
        }
    }
}

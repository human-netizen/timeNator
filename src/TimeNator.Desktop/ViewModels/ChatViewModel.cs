using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.SignalR;
using TimeNator.Desktop.Services;
using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.ViewModels;

public record ChatLine(Guid Id, string Author, string Body, string Time, bool IsMine);

/// <summary>A group's chat: history loaded a page at a time, new messages appended live.</summary>
public partial class ChatViewModel : ViewModelBase, IDisposable
{
    private readonly IApiClient _api;
    private readonly IStudyHubClient _hub;
    private readonly IAuthService _auth;
    private readonly TimeProvider _clock;
    private readonly Guid _groupId;

    public ChatViewModel(IApiClient api, IStudyHubClient hub, IAuthService auth, TimeProvider clock, Guid groupId)
    {
        _api = api;
        _hub = hub;
        _auth = auth;
        _clock = clock;
        _groupId = groupId;
        _hub.MessageReceived += OnMessageReceived;
    }

    /// <summary>Oldest first, so the list reads top to bottom like any chat.</summary>
    public ObservableCollection<ChatLine> Lines { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    public partial string Draft { get; set; } = "";

    [ObservableProperty] public partial bool CanLoadOlder { get; private set; }
    [ObservableProperty] public partial string? Error { get; private set; }

    public override async Task ActivateAsync()
    {
        Lines.Clear();
        await LoadOlderAsync();
    }

    [RelayCommand]
    private async Task LoadOlderAsync()
    {
        try
        {
            var page = await _api.GetGroupMessagesAsync(_groupId, Lines.FirstOrDefault()?.Id);
            foreach (var message in page)
                Lines.Insert(0, ToLine(message));
            CanLoadOlder = page.Count == 50;
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        try
        {
            await _hub.SendMessageAsync(_groupId, Draft);
            Draft = "";
            Error = null;
        }
        catch (HubException ex)
        {
            // The server's reason follows the SignalR wrapper text.
            var marker = ex.Message.IndexOf("HubException: ", StringComparison.Ordinal);
            Error = marker >= 0 ? ex.Message[(marker + 14)..] : ex.Message;
        }
    }

    private bool CanSend() => !string.IsNullOrWhiteSpace(Draft);

    private void OnMessageReceived(Guid groupId, GroupMessageItem message)
    {
        if (groupId == _groupId)
            Dispatcher.UIThread.Post(() => Lines.Add(ToLine(message)));
    }

    private ChatLine ToLine(GroupMessageItem m) =>
        new(m.Id, m.DisplayName, m.Body,
            TimeZoneInfo.ConvertTime(m.SentAt, _clock.LocalTimeZone).ToString("HH:mm"),
            m.UserId == _auth.UserId);

    public void Dispose() => _hub.MessageReceived -= OnMessageReceived;
}

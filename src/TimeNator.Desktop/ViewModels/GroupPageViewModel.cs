using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeNator.Desktop.Services;
using TimeNator.Shared;
using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.ViewModels;

/// <summary>One group as seen by one of its members, with live study status and owner tools.</summary>
public partial class GroupPageViewModel : ViewModelBase, IDisposable
{
    private readonly IApiClient _api;
    private readonly IStudyHubClient _hub;
    private readonly IAuthService _auth;
    private readonly TimeProvider _clock;
    private readonly Action _onRemoved;
    private readonly DispatcherTimer _tick;

    public GroupPageViewModel(IApiClient api, IStudyHubClient hub, IAuthService auth, TimeProvider clock,
        Guid groupId, Action onRemoved)
    {
        _api = api;
        _hub = hub;
        _auth = auth;
        _clock = clock;
        _onRemoved = onRemoved;
        GroupId = groupId;
        Chat = new ChatViewModel(api, hub, auth, clock, groupId);
        _tick = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _tick.Tick += (_, _) => TickAll();
        _hub.MemberStarted += OnMemberStarted;
        _hub.MemberStopped += OnMemberStopped;
        _hub.GroupUpdated += OnGroupUpdated;
        _hub.MemberRemoved += OnMemberRemoved;
    }

    public Guid GroupId { get; }

    public ChatViewModel Chat { get; }

    public ObservableCollection<MemberRowViewModel> Members { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOwner), nameof(Name), nameof(Description), nameof(Announcement),
        nameof(HasAnnouncement), nameof(MinimumText))]
    public partial GroupDetail? Detail { get; private set; }

    [ObservableProperty] public partial string? InviteText { get; private set; }
    [ObservableProperty] public partial string? Error { get; private set; }
    [ObservableProperty] public partial string StudyingText { get; private set; } = "";

    // Owner settings, edited in place and saved together.
    [ObservableProperty] public partial string EditName { get; set; } = "";
    [ObservableProperty] public partial string EditDescription { get; set; } = "";
    [ObservableProperty] public partial string EditAnnouncement { get; set; } = "";
    [ObservableProperty] public partial bool EditChatEnabled { get; set; }
    [ObservableProperty] public partial decimal? EditMinDailyMinutes { get; set; }

    public string Name => Detail?.Name ?? "";
    public string? Description => Detail?.Description;
    public string? Announcement => Detail?.Announcement;
    public bool HasAnnouncement => !string.IsNullOrWhiteSpace(Announcement);
    public bool IsOwner => Detail?.MyRole == GroupRole.Owner;

    public string? MinimumText => Detail?.MinDailySeconds is { } min
        ? $"Daily minimum: {min / 60} minutes"
        : null;

    public override async Task ActivateAsync()
    {
        await LoadAsync();
        _tick.Start();
        await Chat.ActivateAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            Detail = await _api.GetGroupAsync(GroupId);
            var members = await _api.GetGroupMembersAsync(GroupId);
            var presence = (await _api.GetGroupPresenceAsync(GroupId)).ToDictionary(p => p.UserId);

            Members.Clear();
            foreach (var member in members)
                Members.Add(new MemberRowViewModel(member, Detail.MinDailySeconds)
                {
                    Presence = presence.GetValueOrDefault(member.UserId)
                });
            SortMembers();
            TickAll();

            EditName = Detail.Name;
            EditDescription = Detail.Description ?? "";
            EditAnnouncement = Detail.Announcement ?? "";
            EditChatEnabled = Detail.ChatEnabled;
            EditMinDailyMinutes = Detail.MinDailySeconds / 60;
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
    }

    [RelayCommand]
    private Task CreateInviteAsync() => RunAsync(async () =>
    {
        var invite = await _api.CreateInviteAsync(GroupId, new CreateInviteRequest(72, null));
        InviteText = $"Invite code {invite.Code}, valid for 3 days.";
    });

    [RelayCommand]
    private Task LeaveAsync() => RunAsync(async () =>
    {
        if (IsOwner)
            await _api.DeleteGroupAsync(GroupId);
        else
            await _api.LeaveGroupAsync(GroupId);
        await _hub.LeaveGroupChannelAsync(GroupId);
        _onRemoved();
    });

    [RelayCommand]
    private Task SaveSettingsAsync() => RunAsync(async () =>
    {
        var minutes = EditMinDailyMinutes is > 0 ? (int?)EditMinDailyMinutes.Value : null;
        await _api.UpdateGroupAsync(GroupId, new UpdateGroupRequest(EditName.Trim(), EditDescription,
            EditAnnouncement, EditChatEnabled, minutes * 60));
        // The GroupUpdated broadcast reloads the page for everyone, including us.
    });

    [RelayCommand]
    private Task ToggleMuteAsync(MemberRowViewModel row) =>
        RunAsync(() => _api.SetChatPermissionAsync(GroupId, row.UserId, row.IsMuted));

    [RelayCommand]
    private Task KickAsync(MemberRowViewModel row) => RunAsync(() => _api.KickAsync(GroupId, row.UserId));

    [RelayCommand]
    private Task BanAsync(MemberRowViewModel row) => RunAsync(() => _api.BlacklistAsync(GroupId, row.UserId));

    private void OnMemberStarted(Guid groupId, MemberPresence presence)
    {
        if (groupId != GroupId)
            return;
        Dispatcher.UIThread.Post(() =>
        {
            if (Members.FirstOrDefault(m => m.UserId == presence.UserId) is { } row)
                row.Presence = presence;
            SortMembers();
            TickAll();
        });
    }

    private void OnMemberStopped(Guid groupId, Guid userId)
    {
        if (groupId != GroupId)
            return;
        Dispatcher.UIThread.Post(() =>
        {
            if (Members.FirstOrDefault(m => m.UserId == userId) is { } row)
                row.Presence = null;
            SortMembers();
            TickAll();
        });
    }

    private void OnGroupUpdated(Guid groupId)
    {
        if (groupId == GroupId)
            Dispatcher.UIThread.Post(() => _ = LoadAsync());
    }

    private void OnMemberRemoved(Guid groupId, Guid userId)
    {
        if (groupId != GroupId)
            return;
        Dispatcher.UIThread.Post(() =>
        {
            if (userId == _auth.UserId)
            {
                // We were removed: stop hearing this group and leave the page.
                _ = _hub.LeaveGroupChannelAsync(GroupId);
                _onRemoved();
                return;
            }
            if (Members.FirstOrDefault(m => m.UserId == userId) is { } row)
                Members.Remove(row);
            TickAll();
        });
    }

    /// <summary>Studying members first, longest-running on top; then everyone else by name.</summary>
    private void SortMembers()
    {
        var sorted = Members
            .OrderByDescending(m => m.IsStudying)
            .ThenBy(m => m.Presence?.StartedAt ?? DateTimeOffset.MaxValue)
            .ThenBy(m => m.DisplayName)
            .ToList();
        for (var i = 0; i < sorted.Count; i++)
        {
            var current = Members.IndexOf(sorted[i]);
            if (current != i)
                Members.Move(current, i);
        }
    }

    private void TickAll()
    {
        var now = _clock.GetUtcNow();
        foreach (var member in Members)
            member.Tick(now);
        var studying = Members.Count(m => m.IsStudying);
        StudyingText = $"{studying} of {Members.Count} studying now";
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

    public void Dispose()
    {
        _tick.Stop();
        _hub.MemberStarted -= OnMemberStarted;
        _hub.MemberStopped -= OnMemberStopped;
        _hub.GroupUpdated -= OnGroupUpdated;
        _hub.MemberRemoved -= OnMemberRemoved;
        Chat.Dispose();
    }
}

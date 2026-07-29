using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeNator.Desktop.Services;
using TimeNator.Shared;
using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.ViewModels;

/// <summary>One group as seen by one of its members, with live study status.</summary>
public partial class GroupPageViewModel : ViewModelBase, IDisposable
{
    private readonly IApiClient _api;
    private readonly IStudyHubClient _hub;
    private readonly TimeProvider _clock;
    private readonly Action _onRemoved;
    private readonly DispatcherTimer _tick;

    public GroupPageViewModel(IApiClient api, IStudyHubClient hub, IAuthService auth, TimeProvider clock,
        Guid groupId, Action onRemoved)
    {
        _api = api;
        _hub = hub;
        _clock = clock;
        _onRemoved = onRemoved;
        GroupId = groupId;
        Chat = new ChatViewModel(api, hub, auth, clock, groupId);
        _tick = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _tick.Tick += (_, _) => TickAll();
        _hub.MemberStarted += OnMemberStarted;
        _hub.MemberStopped += OnMemberStopped;
    }

    public Guid GroupId { get; }

    public ChatViewModel Chat { get; }

    public ObservableCollection<MemberRowViewModel> Members { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOwner), nameof(Name), nameof(Description), nameof(Announcement),
        nameof(HasAnnouncement))]
    public partial GroupDetail? Detail { get; private set; }

    [ObservableProperty] public partial string? InviteText { get; private set; }
    [ObservableProperty] public partial string? Error { get; private set; }
    [ObservableProperty] public partial string StudyingText { get; private set; } = "";

    public string Name => Detail?.Name ?? "";
    public string? Description => Detail?.Description;
    public string? Announcement => Detail?.Announcement;
    public bool HasAnnouncement => !string.IsNullOrWhiteSpace(Announcement);
    public bool IsOwner => Detail?.MyRole == GroupRole.Owner;

    public override async Task ActivateAsync()
    {
        try
        {
            Detail = await _api.GetGroupAsync(GroupId);
            var members = await _api.GetGroupMembersAsync(GroupId);
            var presence = (await _api.GetGroupPresenceAsync(GroupId)).ToDictionary(p => p.UserId);

            Members.Clear();
            foreach (var member in members)
                Members.Add(new MemberRowViewModel(member) { Presence = presence.GetValueOrDefault(member.UserId) });
            SortMembers();
            TickAll();
            _tick.Start();
            await Chat.ActivateAsync();
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
    }

    [RelayCommand]
    private async Task CreateInviteAsync()
    {
        try
        {
            var invite = await _api.CreateInviteAsync(GroupId, new CreateInviteRequest(72, null));
            InviteText = $"Invite code {invite.Code}, valid for 3 days.";
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
    }

    [RelayCommand]
    private async Task LeaveAsync()
    {
        try
        {
            if (IsOwner)
                await _api.DeleteGroupAsync(GroupId);
            else
                await _api.LeaveGroupAsync(GroupId);
            _onRemoved();
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
    }

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

    public void Dispose()
    {
        _tick.Stop();
        _hub.MemberStarted -= OnMemberStarted;
        _hub.MemberStopped -= OnMemberStopped;
        Chat.Dispose();
    }
}

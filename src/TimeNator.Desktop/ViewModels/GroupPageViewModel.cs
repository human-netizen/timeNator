using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeNator.Desktop.Services;
using TimeNator.Shared;
using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.ViewModels;

/// <summary>One group as seen by one of its members.</summary>
public partial class GroupPageViewModel(IApiClient api, Guid groupId, Action onRemoved) : ViewModelBase
{
    public Guid GroupId { get; } = groupId;

    public ObservableCollection<GroupMemberItem> Members { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOwner), nameof(Name), nameof(Description), nameof(Announcement),
        nameof(HasAnnouncement))]
    public partial GroupDetail? Detail { get; private set; }

    [ObservableProperty] public partial string? InviteText { get; private set; }
    [ObservableProperty] public partial string? Error { get; private set; }

    public string Name => Detail?.Name ?? "";
    public string? Description => Detail?.Description;
    public string? Announcement => Detail?.Announcement;
    public bool HasAnnouncement => !string.IsNullOrWhiteSpace(Announcement);
    public bool IsOwner => Detail?.MyRole == GroupRole.Owner;

    public override async Task ActivateAsync()
    {
        try
        {
            Detail = await api.GetGroupAsync(GroupId);
            Members.Clear();
            foreach (var member in await api.GetGroupMembersAsync(GroupId))
                Members.Add(member);
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
            var invite = await api.CreateInviteAsync(GroupId, new CreateInviteRequest(72, null));
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
                await api.DeleteGroupAsync(GroupId);
            else
                await api.LeaveGroupAsync(GroupId);
            onRemoved();
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
    }
}

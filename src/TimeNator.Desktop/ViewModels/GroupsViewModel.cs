using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeNator.Desktop.Services;
using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.ViewModels;

public enum GroupsPanel
{
    None,
    Create,
    Find,
    Group
}

/// <summary>The groups tab: my groups on the left; create, find, or one group on the right.</summary>
public partial class GroupsViewModel(IApiClient api, IStudyHubClient hub, IAuthService auth, TimeProvider clock)
    : ViewModelBase
{
    public ObservableCollection<GroupSummary> MyGroups { get; } = [];
    public ObservableCollection<GroupSummary> SearchResults { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCreating), nameof(IsFinding), nameof(IsShowingGroup), nameof(IsEmpty))]
    public partial GroupsPanel Panel { get; private set; }

    public bool IsCreating => Panel == GroupsPanel.Create;
    public bool IsFinding => Panel == GroupsPanel.Find;
    public bool IsShowingGroup => Panel == GroupsPanel.Group;
    public bool IsEmpty => Panel == GroupsPanel.None;

    [ObservableProperty] public partial GroupSummary? SelectedGroup { get; set; }
    [ObservableProperty] public partial GroupPageViewModel? CurrentGroup { get; private set; }
    [ObservableProperty] public partial string? Error { get; set; }

    [ObservableProperty] public partial string NewName { get; set; } = "";
    [ObservableProperty] public partial string NewDescription { get; set; } = "";
    [ObservableProperty] public partial bool NewIsPrivate { get; set; }
    [ObservableProperty] public partial string NewPassword { get; set; } = "";

    [ObservableProperty] public partial string Query { get; set; } = "";
    [ObservableProperty] public partial bool HasMoreResults { get; private set; }
    [ObservableProperty] public partial string InviteCode { get; set; } = "";
    [ObservableProperty] public partial string JoinPassword { get; set; } = "";

    private int _page = 1;

    public override Task ActivateAsync() => LoadAsync();

    [RelayCommand]
    private async Task LoadAsync(Guid? select = null)
    {
        await RunAsync(async () =>
        {
            var groups = await api.GetMyGroupsAsync();
            MyGroups.Clear();
            foreach (var group in groups)
                MyGroups.Add(group);
            if (select is { } id)
                SelectedGroup = MyGroups.FirstOrDefault(g => g.Id == id);
        });
    }

    [RelayCommand]
    private void ShowCreate()
    {
        SelectedGroup = null;
        Panel = GroupsPanel.Create;
    }

    [RelayCommand]
    private void ShowFind()
    {
        SelectedGroup = null;
        Panel = GroupsPanel.Find;
    }

    [RelayCommand]
    private Task CreateAsync() => RunAsync(async () =>
    {
        var created = await api.CreateGroupAsync(new CreateGroupRequest(NewName.Trim(), NewDescription.Trim(),
            !NewIsPrivate, NewIsPrivate ? NewPassword : null));
        NewName = NewDescription = NewPassword = "";
        NewIsPrivate = false;
        await hub.JoinMyGroupsAsync();
        await LoadAsync(created.Id);
    });

    [RelayCommand]
    private Task SearchAsync() => RunAsync(async () =>
    {
        _page = 1;
        SearchResults.Clear();
        await LoadPageAsync();
    });

    [RelayCommand]
    private Task MoreResultsAsync() => RunAsync(async () =>
    {
        _page++;
        await LoadPageAsync();
    });

    [RelayCommand]
    private Task JoinAsync(GroupSummary group) => RunAsync(async () =>
    {
        await api.JoinGroupAsync(group.Id, string.IsNullOrEmpty(JoinPassword) ? null : JoinPassword);
        JoinPassword = "";
        await hub.JoinMyGroupsAsync();
        await LoadAsync(group.Id);
    });

    [RelayCommand]
    private Task JoinByCodeAsync() => RunAsync(async () =>
    {
        var joined = await api.AcceptInviteAsync(InviteCode.Trim());
        InviteCode = "";
        await hub.JoinMyGroupsAsync();
        await LoadAsync(joined.Id);
    });

    partial void OnSelectedGroupChanging(GroupSummary? value) => CurrentGroup?.Dispose();

    partial void OnSelectedGroupChanged(GroupSummary? value)
    {
        if (value is null)
        {
            CurrentGroup = null;
            if (Panel == GroupsPanel.Group)
                Panel = GroupsPanel.None;
            return;
        }

        CurrentGroup = new GroupPageViewModel(api, hub, auth, clock, value.Id, () => _ = LoadAsync());
        Panel = GroupsPanel.Group;
        _ = CurrentGroup.ActivateAsync();
    }

    private async Task LoadPageAsync()
    {
        var page = await api.SearchGroupsAsync(Query.Trim(), _page);
        foreach (var group in page.Items)
            SearchResults.Add(group);
        HasMoreResults = page.HasMore;
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
}

using CommunityToolkit.Mvvm.ComponentModel;
using TimeNator.Shared;
using TimeNator.Shared.Dtos;

namespace TimeNator.Desktop.ViewModels;

/// <summary>One member in a group's live view. The running time is computed here, from the broadcast start.</summary>
public partial class MemberRowViewModel(GroupMemberItem member, int? minDailySeconds) : ObservableObject
{
    public GroupMemberItem Member { get; } = member;
    public Guid UserId => Member.UserId;
    public string DisplayName => Member.DisplayName;
    public bool IsOwner => Member.Role == GroupRole.Owner;
    public bool IsMuted => !Member.CanChat;
    public string MuteText => Member.CanChat ? "Mute" : "Unmute";

    /// <summary>Today's total, and whether it meets the group's minimum when there is one.</summary>
    public string TodayText => minDailySeconds is { } min
        ? $"today {Short(Member.TodaySeconds)} / {Short(min)}{(Member.TodaySeconds >= min ? " ✓" : "")}"
        : $"today {Short(Member.TodaySeconds)}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStudying), nameof(SubjectName), nameof(SubjectColorHex))]
    public partial MemberPresence? Presence { get; set; }

    [ObservableProperty] public partial string ElapsedText { get; private set; } = "";

    public bool IsStudying => Presence is not null;
    public string SubjectName => Presence?.SubjectName ?? "Not studying";
    public string SubjectColorHex => Presence?.SubjectColorHex ?? "#555555";

    public void Tick(DateTimeOffset now)
    {
        if (Presence is null)
        {
            ElapsedText = "";
            return;
        }
        var span = now - Presence.StartedAt;
        if (span < TimeSpan.Zero)
            span = TimeSpan.Zero;
        ElapsedText = $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}";
    }

    private static string Short(int seconds)
    {
        var span = TimeSpan.FromSeconds(seconds);
        return span.TotalHours >= 1 ? $"{(int)span.TotalHours}h{span.Minutes:00}" : $"{span.Minutes}m";
    }
}

using TimeNator.Shared;

namespace TimeNator.Api.Entities;

public class GroupMember
{
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public GroupRole Role { get; set; }
    public bool CanChat { get; set; } = true;
    public DateTimeOffset JoinedAt { get; set; }

    public Group Group { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}

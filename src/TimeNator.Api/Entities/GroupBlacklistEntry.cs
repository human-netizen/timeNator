namespace TimeNator.Api.Entities;

/// <summary>A user barred from a group. Checked on every way in: join, password join and invite code.</summary>
public class GroupBlacklistEntry
{
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset BlacklistedAt { get; set; }
    public string? Reason { get; set; }

    public Group Group { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}

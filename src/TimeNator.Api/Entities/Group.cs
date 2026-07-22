namespace TimeNator.Api.Entities;

public class Group
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public string? PasswordHash { get; set; }
    public Guid OwnerId { get; set; }
    public string? Announcement { get; set; }
    public bool ChatEnabled { get; set; } = true;
    public int? MinDailySeconds { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ApplicationUser Owner { get; set; } = null!;
    public List<GroupMember> Members { get; set; } = [];
}

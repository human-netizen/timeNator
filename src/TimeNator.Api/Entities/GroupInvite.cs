namespace TimeNator.Api.Entities;

public class GroupInvite
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid GroupId { get; set; }
    public required string Code { get; set; }
    public Guid CreatedById { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public int? MaxUses { get; set; }
    public int UseCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Group Group { get; set; } = null!;
}

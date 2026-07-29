namespace TimeNator.Api.Entities;

public class GroupMessage
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public required string Body { get; set; }
    public DateTimeOffset SentAt { get; set; }

    public Group Group { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}

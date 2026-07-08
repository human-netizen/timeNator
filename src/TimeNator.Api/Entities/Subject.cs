namespace TimeNator.Api.Entities;

public class Subject
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public required string ColorHex { get; set; }
    public int SortOrder { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
}

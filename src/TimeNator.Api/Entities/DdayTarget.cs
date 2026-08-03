namespace TimeNator.Api.Entities;

/// <summary>A date to count down to, such as an exam.</summary>
public class DdayTarget
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public required string Title { get; set; }
    public DateOnly TargetDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
}

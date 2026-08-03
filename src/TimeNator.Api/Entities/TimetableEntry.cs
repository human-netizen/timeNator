namespace TimeNator.Api.Entities;

/// <summary>A fixed weekly commitment, such as a lecture. Times are local wall-clock times, not instants.</summary>
public class TimetableEntry
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public required string Title { get; set; }
    public Guid? SubjectId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Subject? Subject { get; set; }
}

namespace TimeNator.Api.Entities;

/// <summary>
/// A to-do. A repeating one is a template carrying <see cref="RepeatRule"/>; each day it
/// applies gets its own row pointing back through <see cref="RepeatParentId"/>, so
/// ticking off Monday's copy does not tick off Tuesday's.
/// </summary>
public class TodoItem
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public Guid? SubjectId { get; set; }
    public required string Title { get; set; }
    public DateOnly? DueDate { get; set; }
    public bool IsDone { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? RepeatRule { get; set; }
    public Guid? RepeatParentId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Subject? Subject { get; set; }
    public TodoItem? RepeatParent { get; set; }
}

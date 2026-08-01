namespace TimeNator.Api.Entities;

/// <summary>An application the user may switch to during a session without it counting as a distraction.</summary>
public class AllowedApp
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }

    /// <summary>Executable name without extension, lower-cased, as Process.ProcessName reports it.</summary>
    public required string ProcessName { get; set; }

    public required string DisplayName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
}

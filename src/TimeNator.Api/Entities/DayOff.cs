namespace TimeNator.Api.Entities;

/// <summary>A calendar day the user chose not to study. A date, not an instant.</summary>
public class DayOff
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
}

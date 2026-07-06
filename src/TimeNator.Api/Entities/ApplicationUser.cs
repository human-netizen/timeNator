using Microsoft.AspNetCore.Identity;

namespace TimeNator.Api.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public required string DisplayName { get; set; }
    public string? StatusMessage { get; set; }
    public string? AvatarKey { get; set; }
    public string? ThemeKey { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

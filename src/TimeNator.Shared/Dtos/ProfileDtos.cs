namespace TimeNator.Shared.Dtos;

public record ProfileResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    string? StatusMessage,
    string? AvatarKey,
    string? ThemeKey);

public record UpdateProfileRequest(string DisplayName, string? StatusMessage, string? AvatarKey, string? ThemeKey);

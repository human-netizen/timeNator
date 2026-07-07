namespace TimeNator.Shared.Dtos;

public record RegisterRequest(string Email, string Password, string DisplayName);

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

public record AuthResponse(
    Guid UserId,
    string DisplayName,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken);

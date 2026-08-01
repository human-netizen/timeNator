namespace TimeNator.Shared.Dtos;

public record AllowedAppRequest(string ProcessName, string DisplayName);

public record AllowedAppResponse(Guid Id, string ProcessName, string DisplayName);

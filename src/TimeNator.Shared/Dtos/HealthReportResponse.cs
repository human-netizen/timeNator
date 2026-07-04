namespace TimeNator.Shared.Dtos;

public record HealthReportResponse(string Status, Dictionary<string, string> Checks, int DurationMs);

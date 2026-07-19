namespace TimeNator.Shared.Dtos;

public record DayOffRequest(DateOnly Date, string? Note);

public record DayOffResponse(DateOnly Date, string? Note);

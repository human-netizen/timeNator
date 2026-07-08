namespace TimeNator.Shared.Dtos;

public record CreateSubjectRequest(string Name, string ColorHex);

public record UpdateSubjectRequest(string Name, string ColorHex, int SortOrder);

public record SubjectResponse(Guid Id, string Name, string ColorHex, int SortOrder, bool IsArchived);

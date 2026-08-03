using Microsoft.EntityFrameworkCore;
using TimeNator.Api.Data;
using TimeNator.Api.Entities;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Services;

public class TimetableService(AppDbContext db, TimeProvider clock)
{
    public Task<List<TimetableResponse>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        db.TimetableEntries
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.DayOfWeek).ThenBy(t => t.StartTime)
            .Select(t => ToResponse(t))
            .ToListAsync(cancellationToken);

    public async Task<ServiceResult<TimetableResponse>> CreateAsync(Guid userId, TimetableRequest request,
        CancellationToken cancellationToken)
    {
        if (await ValidateAsync(userId, request, cancellationToken) is { } error)
            return error;

        var entry = new TimetableEntry { UserId = userId, Title = "", CreatedAt = clock.GetUtcNow() };
        Apply(entry, request);
        db.TimetableEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<TimetableResponse>.Ok(await FindAsync(entry.Id, cancellationToken));
    }

    public async Task<ServiceResult<TimetableResponse>> UpdateAsync(Guid userId, Guid id, TimetableRequest request,
        CancellationToken cancellationToken)
    {
        var entry = await db.TimetableEntries.SingleOrDefaultAsync(t => t.Id == id && t.UserId == userId,
            cancellationToken);
        if (entry is null)
            return ServiceResult<TimetableResponse>.Fail(ServiceError.NotFound, "Timetable entry not found.");
        if (await ValidateAsync(userId, request, cancellationToken) is { } error)
            return error;

        Apply(entry, request);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<TimetableResponse>.Ok(await FindAsync(entry.Id, cancellationToken));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken) =>
        await db.TimetableEntries.Where(t => t.Id == id && t.UserId == userId).ExecuteDeleteAsync(cancellationToken) > 0
            ? ServiceResult<bool>.Ok(true)
            : ServiceResult<bool>.Fail(ServiceError.NotFound, "Timetable entry not found.");

    private async Task<ServiceResult<TimetableResponse>?> ValidateAsync(Guid userId, TimetableRequest request,
        CancellationToken cancellationToken)
    {
        if (request.StartTime >= request.EndTime)
            return Invalid("The entry must end after it starts.");
        if (request.Title?.Trim().Length is not (> 0 and <= 100))
            return Invalid("The title must be 1 to 100 characters.");
        if (!Enum.IsDefined(request.DayOfWeek))
            return Invalid("Unknown day of the week.");
        if (request.SubjectId is { } id &&
            !await db.Subjects.AnyAsync(s => s.Id == id && s.UserId == userId, cancellationToken))
            return Invalid("Unknown subject.");
        return null;
    }

    private static void Apply(TimetableEntry entry, TimetableRequest request)
    {
        entry.DayOfWeek = request.DayOfWeek;
        entry.StartTime = request.StartTime;
        entry.EndTime = request.EndTime;
        entry.Title = request.Title.Trim();
        entry.SubjectId = request.SubjectId;
    }

    private Task<TimetableResponse> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.TimetableEntries.Where(t => t.Id == id).Select(t => ToResponse(t)).SingleAsync(cancellationToken);

    private static ServiceResult<TimetableResponse> Invalid(string message) =>
        ServiceResult<TimetableResponse>.Fail(ServiceError.Invalid, message);

    private static TimetableResponse ToResponse(TimetableEntry t) =>
        new(t.Id, t.DayOfWeek, t.StartTime, t.EndTime, t.Title, t.SubjectId,
            t.Subject != null ? t.Subject.ColorHex : null);
}

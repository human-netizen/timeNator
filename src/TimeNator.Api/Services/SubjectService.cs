using Microsoft.EntityFrameworkCore;
using TimeNator.Api.Data;
using TimeNator.Api.Entities;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Services;

public class SubjectService(AppDbContext db, TimeProvider clock)
{
    public Task<List<SubjectResponse>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Subjects
            .Where(s => s.UserId == userId && !s.IsArchived)
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Name)
            .Select(s => ToResponse(s))
            .ToListAsync(cancellationToken);

    public Task<SubjectResponse?> FindAsync(Guid userId, Guid id, CancellationToken cancellationToken) =>
        db.Subjects
            .Where(s => s.Id == id && s.UserId == userId && !s.IsArchived)
            .Select(s => ToResponse(s))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<bool> NameTakenAsync(Guid userId, string name, Guid? exceptId, CancellationToken cancellationToken) =>
        db.Subjects.AnyAsync(
            s => s.UserId == userId && !s.IsArchived && s.Name == name && s.Id != exceptId,
            cancellationToken);

    public async Task<SubjectResponse> CreateAsync(
        Guid userId, CreateSubjectRequest request, CancellationToken cancellationToken)
    {
        var nextOrder = await db.Subjects
            .Where(s => s.UserId == userId && !s.IsArchived)
            .MaxAsync(s => (int?)s.SortOrder, cancellationToken) ?? -1;

        var subject = new Subject
        {
            UserId = userId,
            Name = request.Name.Trim(),
            ColorHex = request.ColorHex.ToUpperInvariant(),
            SortOrder = nextOrder + 1,
            CreatedAt = clock.GetUtcNow()
        };
        db.Subjects.Add(subject);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(subject);
    }

    public async Task<SubjectResponse?> UpdateAsync(
        Guid userId, Guid id, UpdateSubjectRequest request, CancellationToken cancellationToken)
    {
        var subject = await db.Subjects
            .SingleOrDefaultAsync(s => s.Id == id && s.UserId == userId && !s.IsArchived, cancellationToken);
        if (subject is null)
            return null;

        subject.Name = request.Name.Trim();
        subject.ColorHex = request.ColorHex.ToUpperInvariant();
        subject.SortOrder = request.SortOrder;
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(subject);
    }

    // Subjects are archived rather than deleted so past sessions keep their subject.
    public async Task<bool> ArchiveAsync(Guid userId, Guid id, CancellationToken cancellationToken) =>
        await db.Subjects
            .Where(s => s.Id == id && s.UserId == userId && !s.IsArchived)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsArchived, true), cancellationToken) > 0;

    private static SubjectResponse ToResponse(Subject s) =>
        new(s.Id, s.Name, s.ColorHex, s.SortOrder, s.IsArchived);
}

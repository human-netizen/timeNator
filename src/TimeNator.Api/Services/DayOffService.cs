using Microsoft.EntityFrameworkCore;
using TimeNator.Api.Data;
using TimeNator.Api.Entities;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Services;

public class DayOffService(AppDbContext db, TimeProvider clock)
{
    public Task<List<DayOffResponse>> ListAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        db.DayOffs
            .Where(d => d.UserId == userId && d.Date >= from && d.Date <= to)
            .OrderBy(d => d.Date)
            .Select(d => new DayOffResponse(d.Date, d.Note))
            .ToListAsync(cancellationToken);

    /// <summary>Throws <see cref="DbUpdateException"/> when the day is already marked; the unique index decides.</summary>
    public async Task<DayOffResponse> CreateAsync(Guid userId, DayOffRequest request,
        CancellationToken cancellationToken)
    {
        var dayOff = new DayOff
        {
            UserId = userId,
            Date = request.Date,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            CreatedAt = clock.GetUtcNow()
        };
        db.DayOffs.Add(dayOff);
        await db.SaveChangesAsync(cancellationToken);
        return new DayOffResponse(dayOff.Date, dayOff.Note);
    }

    public async Task<bool> DeleteAsync(Guid userId, DateOnly date, CancellationToken cancellationToken) =>
        await db.DayOffs
            .Where(d => d.UserId == userId && d.Date == date)
            .ExecuteDeleteAsync(cancellationToken) > 0;
}

using Microsoft.EntityFrameworkCore;
using TimeNator.Api.Data;
using TimeNator.Api.Entities;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Services;

public class DdayService(AppDbContext db, TimeProvider clock)
{
    public Task<List<DdayResponse>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        db.DdayTargets
            .Where(d => d.UserId == userId)
            .OrderBy(d => d.TargetDate)
            .Select(d => new DdayResponse(d.Id, d.Title, d.TargetDate))
            .ToListAsync(cancellationToken);

    public async Task<DdayResponse> CreateAsync(Guid userId, DdayRequest request, CancellationToken cancellationToken)
    {
        var dday = new DdayTarget
        {
            UserId = userId,
            Title = request.Title.Trim(),
            TargetDate = request.TargetDate,
            CreatedAt = clock.GetUtcNow()
        };
        db.DdayTargets.Add(dday);
        await db.SaveChangesAsync(cancellationToken);
        return new DdayResponse(dday.Id, dday.Title, dday.TargetDate);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken) =>
        await db.DdayTargets.Where(d => d.Id == id && d.UserId == userId).ExecuteDeleteAsync(cancellationToken) > 0;
}

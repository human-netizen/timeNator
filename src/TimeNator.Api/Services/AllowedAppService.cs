using Microsoft.EntityFrameworkCore;
using TimeNator.Api.Data;
using TimeNator.Api.Entities;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Services;

public class AllowedAppService(AppDbContext db, TimeProvider clock)
{
    public Task<List<AllowedAppResponse>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        db.AllowedApps
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.DisplayName)
            .Select(a => new AllowedAppResponse(a.Id, a.ProcessName, a.DisplayName))
            .ToListAsync(cancellationToken);

    public async Task<ServiceResult<AllowedAppResponse>> AddAsync(Guid userId, AllowedAppRequest request,
        CancellationToken cancellationToken)
    {
        var processName = request.ProcessName.Trim().ToLowerInvariant();
        if (processName.EndsWith(".exe"))
            processName = processName[..^4];
        if (processName.Length is 0 or > 100 || request.DisplayName.Trim().Length is 0 or > 100)
            return ServiceResult<AllowedAppResponse>.Fail(ServiceError.Invalid,
                "Process and display names must be 1 to 100 characters.");
        if (await db.AllowedApps.AnyAsync(a => a.UserId == userId && a.ProcessName == processName, cancellationToken))
            return ServiceResult<AllowedAppResponse>.Fail(ServiceError.Conflict, "That app is already allowed.");

        var app = new AllowedApp
        {
            UserId = userId,
            ProcessName = processName,
            DisplayName = request.DisplayName.Trim(),
            CreatedAt = clock.GetUtcNow()
        };
        db.AllowedApps.Add(app);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<AllowedAppResponse>.Ok(new AllowedAppResponse(app.Id, app.ProcessName, app.DisplayName));
    }

    public async Task<ServiceResult<bool>> RemoveAsync(Guid userId, Guid id, CancellationToken cancellationToken) =>
        await db.AllowedApps.Where(a => a.Id == id && a.UserId == userId).ExecuteDeleteAsync(cancellationToken) > 0
            ? ServiceResult<bool>.Ok(true)
            : ServiceResult<bool>.Fail(ServiceError.NotFound, "Allowed app not found.");
}

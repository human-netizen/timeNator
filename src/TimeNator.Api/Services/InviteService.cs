using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TimeNator.Api.Data;
using TimeNator.Api.Entities;
using TimeNator.Shared.Dtos;

namespace TimeNator.Api.Services;

/// <summary>
/// Invite codes let someone into a group without its password; holding a code is the
/// proof of invitation. Codes can expire and can be capped at a number of uses.
/// </summary>
public class InviteService(AppDbContext db, GroupService groups, TimeProvider clock)
{
    // No 0/O or 1/I, so a code read aloud or copied by hand survives.
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 8;

    public async Task<ServiceResult<InviteResponse>> CreateAsync(Guid userId, Guid groupId,
        CreateInviteRequest request, CancellationToken cancellationToken)
    {
        if (!await groups.IsMemberAsync(userId, groupId, cancellationToken))
            return ServiceResult<InviteResponse>.Fail(ServiceError.NotFound, "Group not found.");
        if (request.ExpiresInHours is <= 0 or > 24 * 30 || request.MaxUses is <= 0 or > 1000)
            return ServiceResult<InviteResponse>.Fail(ServiceError.Invalid,
                "Expiry must be 1 to 720 hours and uses 1 to 1000.");

        var now = clock.GetUtcNow();
        var invite = new GroupInvite
        {
            GroupId = groupId,
            Code = RandomNumberGenerator.GetString(Alphabet, CodeLength),
            CreatedById = userId,
            ExpiresAt = request.ExpiresInHours is { } hours ? now.AddHours(hours) : null,
            MaxUses = request.MaxUses,
            CreatedAt = now
        };
        db.GroupInvites.Add(invite);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<InviteResponse>.Ok(new InviteResponse(invite.Code, invite.ExpiresAt, invite.MaxUses, 0));
    }

    public async Task<ServiceResult<GroupSummary>> PreviewAsync(string code, CancellationToken cancellationToken)
    {
        var invite = await FindUsableAsync(code, cancellationToken);
        if (invite is null)
            return ServiceResult<GroupSummary>.Fail(ServiceError.NotFound, "This invite is invalid or has expired.");

        var summary = await db.Groups
            .Where(g => g.Id == invite.GroupId)
            .Select(g => new GroupSummary(g.Id, g.Name, g.Description, g.IsPublic, g.Members.Count,
                g.Owner.DisplayName))
            .SingleAsync(cancellationToken);
        return ServiceResult<GroupSummary>.Ok(summary);
    }

    public async Task<ServiceResult<GroupDetail>> AcceptAsync(Guid userId, string code,
        CancellationToken cancellationToken)
    {
        var invite = await FindUsableAsync(code, cancellationToken);
        if (invite is null)
            return ServiceResult<GroupDetail>.Fail(ServiceError.NotFound, "This invite is invalid or has expired.");

        var joined = await groups.AddMemberAsync(userId, invite.Group, cancellationToken);
        if (joined.Error is null)
        {
            // Atomic increment, so two people redeeming the last use at once cannot both count.
            await db.GroupInvites
                .Where(i => i.Id == invite.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.UseCount, i => i.UseCount + 1), cancellationToken);
        }
        return joined;
    }

    private Task<GroupInvite?> FindUsableAsync(string code, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var normalized = code.Trim().ToUpperInvariant();
        return db.GroupInvites
            .Include(i => i.Group)
            .Where(i => i.Code == normalized
                        && (i.ExpiresAt == null || i.ExpiresAt > now)
                        && (i.MaxUses == null || i.UseCount < i.MaxUses))
            .SingleOrDefaultAsync(cancellationToken);
    }
}

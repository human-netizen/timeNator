using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TimeNator.Api.Entities;

namespace TimeNator.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<StudySession> StudySessions => Set<StudySession>();
    public DbSet<DayOff> DayOffs => Set<DayOff>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<GroupInvite> GroupInvites => Set<GroupInvite>();
    public DbSet<GroupMessage> GroupMessages => Set<GroupMessage>();
    public DbSet<GroupBlacklistEntry> GroupBlacklist => Set<GroupBlacklistEntry>();
    public DbSet<AllowedApp> AllowedApps => Set<AllowedApp>();
    public DbSet<DistractionEvent> DistractionEvents => Set<DistractionEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

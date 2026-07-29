using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeNator.Api.Entities;

namespace TimeNator.Api.Data.Configurations;

public class GroupBlacklistEntryConfiguration : IEntityTypeConfiguration<GroupBlacklistEntry>
{
    public void Configure(EntityTypeBuilder<GroupBlacklistEntry> builder)
    {
        builder.HasKey(b => new { b.GroupId, b.UserId });
        builder.HasIndex(b => b.UserId);
        builder.Property(b => b.Reason).HasMaxLength(200);

        builder.HasOne(b => b.Group)
            .WithMany()
            .HasForeignKey(b => b.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

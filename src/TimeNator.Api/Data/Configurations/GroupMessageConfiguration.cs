using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeNator.Api.Entities;

namespace TimeNator.Api.Data.Configurations;

public class GroupMessageConfiguration : IEntityTypeConfiguration<GroupMessage>
{
    public void Configure(EntityTypeBuilder<GroupMessage> builder)
    {
        builder.Property(m => m.Body).HasMaxLength(GroupMessageRules.MaxLength).IsRequired();

        // History is read newest first, one page at a time, per group.
        builder.HasIndex(m => new { m.GroupId, m.SentAt }).IsDescending(false, true);
        builder.HasIndex(m => m.UserId);

        builder.HasOne(m => m.Group)
            .WithMany()
            .HasForeignKey(m => m.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public static class GroupMessageRules
{
    public const int MaxLength = 1000;
}

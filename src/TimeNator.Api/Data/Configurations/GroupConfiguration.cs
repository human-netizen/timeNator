using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeNator.Api.Entities;

namespace TimeNator.Api.Data.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.Property(g => g.Name).HasMaxLength(60).IsRequired();
        builder.Property(g => g.Description).HasMaxLength(500);
        builder.Property(g => g.PasswordHash).HasMaxLength(200);
        builder.Property(g => g.Announcement).HasMaxLength(500);

        builder.HasIndex(g => g.Name);
        builder.HasIndex(g => g.OwnerId);

        builder.HasOne(g => g.Owner)
            .WithMany()
            .HasForeignKey(g => g.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

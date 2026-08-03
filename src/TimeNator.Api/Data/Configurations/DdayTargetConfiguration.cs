using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeNator.Api.Entities;

namespace TimeNator.Api.Data.Configurations;

public class DdayTargetConfiguration : IEntityTypeConfiguration<DdayTarget>
{
    public void Configure(EntityTypeBuilder<DdayTarget> builder)
    {
        builder.Property(d => d.Title).HasMaxLength(100).IsRequired();
        builder.HasIndex(d => new { d.UserId, d.TargetDate });

        builder.HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

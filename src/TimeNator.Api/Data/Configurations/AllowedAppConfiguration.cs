using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeNator.Api.Entities;

namespace TimeNator.Api.Data.Configurations;

public class AllowedAppConfiguration : IEntityTypeConfiguration<AllowedApp>
{
    public void Configure(EntityTypeBuilder<AllowedApp> builder)
    {
        builder.Property(a => a.ProcessName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.DisplayName).HasMaxLength(100).IsRequired();
        builder.HasIndex(a => new { a.UserId, a.ProcessName }).IsUnique();

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

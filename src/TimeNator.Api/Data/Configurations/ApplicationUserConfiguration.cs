using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeNator.Api.Entities;

namespace TimeNator.Api.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.DisplayName).HasMaxLength(50).IsRequired();
        builder.Property(u => u.StatusMessage).HasMaxLength(200);
        builder.Property(u => u.AvatarKey).HasMaxLength(50);
        builder.Property(u => u.ThemeKey).HasMaxLength(50);
    }
}

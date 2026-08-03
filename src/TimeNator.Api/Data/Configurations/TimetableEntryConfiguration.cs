using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeNator.Api.Entities;

namespace TimeNator.Api.Data.Configurations;

public class TimetableEntryConfiguration : IEntityTypeConfiguration<TimetableEntry>
{
    public void Configure(EntityTypeBuilder<TimetableEntry> builder)
    {
        builder.Property(t => t.Title).HasMaxLength(100).IsRequired();
        builder.HasIndex(t => new { t.UserId, t.DayOfWeek });
        builder.HasIndex(t => t.SubjectId);
        builder.ToTable(t => t.HasCheckConstraint("CK_TimetableEntries_StartBeforeEnd",
            "\"StartTime\" < \"EndTime\""));

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Subject)
            .WithMany()
            .HasForeignKey(t => t.SubjectId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

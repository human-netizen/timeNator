using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeNator.Api.Entities;

namespace TimeNator.Api.Data.Configurations;

public class StudySessionConfiguration : IEntityTypeConfiguration<StudySession>
{
    public void Configure(EntityTypeBuilder<StudySession> builder)
    {
        builder.Property(s => s.Mode).HasConversion<string>().HasMaxLength(16);
        builder.Property(s => s.Source).HasConversion<string>().HasMaxLength(16);

        builder.HasIndex(s => new { s.UserId, s.StartedAt });
        builder.HasIndex(s => new { s.UserId, s.SubjectId, s.StartedAt });
        builder.HasIndex(s => s.SubjectId);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Subject)
            .WithMany()
            .HasForeignKey(s => s.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

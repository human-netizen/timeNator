using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeNator.Api.Entities;

namespace TimeNator.Api.Data.Configurations;

public class DistractionEventConfiguration : IEntityTypeConfiguration<DistractionEvent>
{
    public void Configure(EntityTypeBuilder<DistractionEvent> builder)
    {
        builder.Property(e => e.ProcessName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.WindowTitle).HasMaxLength(300).IsRequired();
        builder.HasIndex(e => new { e.UserId, e.OccurredAt });
        builder.HasIndex(e => e.SessionId);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Deleting a session keeps its distractions; they simply lose the link.
        builder.HasOne(e => e.Session)
            .WithMany()
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

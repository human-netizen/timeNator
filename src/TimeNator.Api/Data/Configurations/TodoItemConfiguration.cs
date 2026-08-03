using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeNator.Api.Entities;

namespace TimeNator.Api.Data.Configurations;

public class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.Property(t => t.Title).HasMaxLength(200).IsRequired();
        builder.Property(t => t.RepeatRule).HasMaxLength(40);

        builder.HasIndex(t => new { t.UserId, t.DueDate });
        builder.HasIndex(t => t.SubjectId);
        // One instance per template per day; also the lookup when materialising a day.
        builder.HasIndex(t => new { t.RepeatParentId, t.DueDate }).IsUnique();

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Subject)
            .WithMany()
            .HasForeignKey(t => t.SubjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.RepeatParent)
            .WithMany()
            .HasForeignKey(t => t.RepeatParentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeNator.Api.Entities;

namespace TimeNator.Api.Data.Configurations;

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.Property(s => s.Name).HasMaxLength(50).IsRequired();
        builder.Property(s => s.ColorHex).HasMaxLength(7).IsFixedLength().IsRequired();

        // Archived subjects keep their name so old sessions still read correctly,
        // and a new subject may reuse it.
        builder.HasIndex(s => new { s.UserId, s.Name })
            .IsUnique()
            .HasFilter("\"IsArchived\" = false");

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

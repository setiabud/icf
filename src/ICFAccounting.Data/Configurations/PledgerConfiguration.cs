using ICFAccounting.Domain.Pledgers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ICFAccounting.Data.Configurations;

internal sealed class PledgerConfiguration : IEntityTypeConfiguration<Pledger>
{
    public void Configure(EntityTypeBuilder<Pledger> builder)
    {
        builder.ToTable("Pledgers");
        builder.HasKey(pledger => pledger.Id);
        builder
            .Property(pledger => pledger.Id)
            .HasConversion(identity => identity.Value, value => new PledgerId(value))
            .ValueGeneratedNever();
        builder.Property(pledger => pledger.PledgerName).HasColumnType("text").IsRequired();
        builder.Property(pledger => pledger.Email).HasColumnType("text");
        builder
            .Property(pledger => pledger.EnableNotification)
            .HasColumnType("boolean")
            .HasDefaultValue(false);
        builder
            .Property(pledger => pledger.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .HasConversion<UtcDateTimeConverter>();
        builder
            .Property(pledger => pledger.UpdatedAt)
            .HasColumnType("timestamp without time zone")
            .HasConversion<UtcDateTimeConverter>();
    }
}

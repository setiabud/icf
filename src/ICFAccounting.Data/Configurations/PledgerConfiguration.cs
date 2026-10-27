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
        builder.Property(pledger => pledger.Id)
            .HasConversion(identity => identity.Value, value => new PledgerId(value))
            .ValueGeneratedNever();
        builder.Property(pledger => pledger.PledgerName)
            .HasColumnType("nvarchar(max)")
            .IsRequired();
        builder.Property(pledger => pledger.CreatedAt)
            .HasColumnType("datetime2(7)")
            .HasConversion<UtcDateTimeConverter>();
        builder.Property(pledger => pledger.UpdatedAt)
            .HasColumnType("datetime2(7)")
            .HasConversion<UtcDateTimeConverter>();
    }
}
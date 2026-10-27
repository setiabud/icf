using ICFAccounting.Domain.PledgeCategories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ICFAccounting.Data.Configurations;

internal sealed class PledgeCategoryConfiguration : IEntityTypeConfiguration<PledgeCategory>
{
    public void Configure(EntityTypeBuilder<PledgeCategory> builder)
    {
        builder.ToTable("PledgeCategories");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Id)
            .HasConversion(identity => identity.Value, value => new PledgeCategoryId(value))
            .ValueGeneratedNever();
        builder.Property(category => category.CategoryName)
            .HasColumnType("nvarchar(max)")
            .IsRequired();
        builder.Property(category => category.CreatedAt)
            .HasColumnType("datetime2(7)")
            .HasConversion<UtcDateTimeConverter>();
        builder.Property(category => category.UpdatedAt)
            .HasColumnType("datetime2(7)")
            .HasConversion<UtcDateTimeConverter>();
    }
}
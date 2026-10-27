using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Pledges;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ICFAccounting.Data.Configurations;

internal sealed class PledgeConfiguration : IEntityTypeConfiguration<Pledge>
{
    public void Configure(EntityTypeBuilder<Pledge> builder)
    {
        builder.ToTable("Pledges");
        builder.HasKey(pledge => pledge.Id);
        builder.Property(pledge => pledge.Id)
            .HasConversion(identity => identity.Value, value => new PledgeId(value))
            .ValueGeneratedNever();
        builder.Property(pledge => pledge.PledgerId)
            .HasConversion(identity => identity.Value, value => new PledgerId(value));
        builder.Property(pledge => pledge.PledgeCategoryId)
            .HasConversion(identity => identity.Value, value => new PledgeCategoryId(value));
        builder.Property(pledge => pledge.Amount).HasPrecision(38, 2)
            .Metadata.SetTypeMapping(new AccountingAmountTypeMapping());
        builder.Property(pledge => pledge.Comment).HasColumnType("nvarchar(max)");
        builder.Property(pledge => pledge.CreatedAt)
            .HasColumnType("datetime2(7)")
            .HasConversion<UtcDateTimeConverter>();
        builder.Property(pledge => pledge.UpdatedAt)
            .HasColumnType("datetime2(7)")
            .HasConversion<UtcDateTimeConverter>();

        builder.HasOne<Pledger>().WithMany()
            .HasForeignKey(pledge => pledge.PledgerId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<PledgeCategory>().WithMany()
            .HasForeignKey(pledge => pledge.PledgeCategoryId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(pledge => new { pledge.PledgerId, pledge.Year, pledge.Id })
            .IsDescending(false, true, true);
        builder.HasIndex(pledge => new { pledge.PledgeCategoryId, pledge.Year, pledge.Id })
            .IsDescending(false, true, true);
    }
}
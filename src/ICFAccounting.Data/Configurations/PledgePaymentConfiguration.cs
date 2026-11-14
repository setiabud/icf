using ICFAccounting.Domain.PledgePayments;
using ICFAccounting.Domain.Pledges;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ICFAccounting.Data.Configurations;

internal sealed class PledgePaymentConfiguration : IEntityTypeConfiguration<PledgePayment>
{
    public void Configure(EntityTypeBuilder<PledgePayment> builder)
    {
        builder.ToTable("PledgePayments");
        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Id)
            .HasConversion(identity => identity.Value, value => new PledgePaymentId(value))
            .ValueGeneratedNever();
        builder.Property(payment => payment.PledgeId)
            .HasConversion(identity => identity.Value, value => new PledgeId(value));
        builder.Property(payment => payment.Amount).HasColumnType("numeric");
        builder.Property(payment => payment.PaymentDate)
            .HasColumnType("timestamp without time zone")
            .HasConversion<PaymentDateConverter>();
        builder.Property(payment => payment.Comment).HasColumnType("text");
        builder.Property(payment => payment.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .HasConversion<UtcDateTimeConverter>();
        builder.Property(payment => payment.UpdatedAt)
            .HasColumnType("timestamp without time zone")
            .HasConversion<UtcDateTimeConverter>();

        builder.HasOne<Pledge>().WithMany()
            .HasForeignKey(payment => payment.PledgeId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(payment => new { payment.PledgeId, payment.PaymentDate, payment.Id })
            .IsDescending(false, true, true);
    }
}
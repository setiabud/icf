using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ICFAccounting.Data.Configurations;

internal sealed class PaymentDateConverter : ValueConverter<DateTime, DateTime>
{
    public PaymentDateConverter()
        : base(
            timestamp => DateTime.SpecifyKind(timestamp, DateTimeKind.Unspecified),
            timestamp => DateTime.SpecifyKind(timestamp, DateTimeKind.Unspecified)
        ) { }
}

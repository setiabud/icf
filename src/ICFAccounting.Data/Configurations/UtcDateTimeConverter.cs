using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ICFAccounting.Data.Configurations;

internal sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(timestamp => timestamp, timestamp => DateTime.SpecifyKind(timestamp, DateTimeKind.Utc))
    {
    }
}
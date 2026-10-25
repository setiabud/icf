using ICFAccounting.Domain.PledgePayments;
using ICFAccounting.Domain.Pledges;
using Microsoft.Extensions.Time.Testing;

namespace ICFAccounting.UnitTests.Domain;

public sealed class PledgePaymentTests
{
    private static readonly DateTimeOffset StartTime = new(2026, 1, 15, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Create_initializes_identity_payment_fields_and_audit_fields()
    {
        var pledgeId = PledgeId.New();
        var paymentDate = new DateTime(2026, 1, 10);
        var clock = new FakeTimeProvider(StartTime);

        var payment = PledgePayment.Create(pledgeId, 123.45m, paymentDate, "  Monthly payment  ", clock);

        Assert.NotEqual(Guid.Empty, payment.Id.Value);
        Assert.Equal(pledgeId, payment.PledgeId);
        Assert.Equal(123.45m, payment.Amount);
        Assert.Equal(paymentDate, payment.PaymentDate);
        Assert.Equal("Monthly payment", payment.Comment);
        Assert.True(payment.Active);
        Assert.Equal(StartTime.UtcDateTime, payment.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, payment.CreatedAt.Kind);
        Assert.Equal(payment.CreatedAt, payment.UpdatedAt);
        Assert.NotEqual(payment.Id, PledgePayment.Create(pledgeId, 1m, paymentDate).Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    public void Create_normalizes_empty_comment_to_null(string? comment)
    {
        var payment = PledgePayment.Create(PledgeId.New(), 1m, StartTime.UtcDateTime, comment);

        Assert.Null(payment.Comment);
    }

    [Fact]
    public void Create_without_clock_uses_current_utc_time()
    {
        var before = DateTime.UtcNow;

        var payment = PledgePayment.Create(PledgeId.New(), 1m, StartTime.UtcDateTime);

        Assert.Equal(DateTimeKind.Utc, payment.CreatedAt.Kind);
        Assert.InRange(payment.CreatedAt, before, DateTime.UtcNow);
        Assert.Equal(payment.CreatedAt, payment.UpdatedAt);
    }

    public static TheoryData<decimal, string> InvalidAmounts => new()
    {
        { 0m, "amount" },
        { -1m, "amount" },
        { 1.001m, "amount" }
    };

    [Theory]
    [MemberData(nameof(InvalidAmounts))]
    public void Create_with_invalid_amount_throws(decimal amount, string parameter)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(
            () => PledgePayment.Create(PledgeId.New(), amount, StartTime.UtcDateTime));

        Assert.Equal(parameter, exception.ParamName);
    }

    [Fact]
    public void Create_with_empty_pledge_id_throws()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => PledgePayment.Create(default, 1m, StartTime.UtcDateTime));

        Assert.Equal("pledgeId", exception.ParamName);
    }

    [Fact]
    public void Create_with_default_payment_date_throws()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => PledgePayment.Create(PledgeId.New(), 1m, default));

        Assert.Equal("paymentDate", exception.ParamName);
    }

    [Fact]
    public void Update_changes_payment_fields_and_preserves_identity_and_pledge()
    {
        var clock = new FakeTimeProvider(StartTime);
        var pledgeId = PledgeId.New();
        var paymentDate = new DateTime(2026, 1, 10);
        var payment = PledgePayment.Create(pledgeId, 10m, paymentDate, "Original", clock);
        var originalId = payment.Id;
        var originalCreatedAt = payment.CreatedAt;
        clock.Advance(TimeSpan.FromMinutes(5));

        payment.Update(25.50m, new DateTime(2026, 1, 20), "  Updated  ", false, clock);

        Assert.Equal(originalId, payment.Id);
        Assert.Equal(pledgeId, payment.PledgeId);
        Assert.Equal(25.50m, payment.Amount);
        Assert.Equal(new DateTime(2026, 1, 20), payment.PaymentDate);
        Assert.Equal("Updated", payment.Comment);
        Assert.False(payment.Active);
        Assert.Equal(originalCreatedAt, payment.CreatedAt);
        Assert.Equal(StartTime.AddMinutes(5).UtcDateTime, payment.UpdatedAt);
    }

    [Fact]
    public void Update_with_invalid_amount_throws_and_preserves_state()
    {
        var clock = new FakeTimeProvider(StartTime);
        var payment = PledgePayment.Create(PledgeId.New(), 10m, StartTime.UtcDateTime, "Original", clock);
        var originalId = payment.Id;
        var originalPledgeId = payment.PledgeId;
        var originalCreatedAt = payment.CreatedAt;
        var originalUpdatedAt = payment.UpdatedAt;
        clock.Advance(TimeSpan.FromMinutes(5));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => payment.Update(0m, StartTime.UtcDateTime, "Changed", false, clock));

        Assert.Equal(originalId, payment.Id);
        Assert.Equal(originalPledgeId, payment.PledgeId);
        Assert.Equal(10m, payment.Amount);
        Assert.Equal(StartTime.UtcDateTime, payment.PaymentDate);
        Assert.Equal("Original", payment.Comment);
        Assert.True(payment.Active);
        Assert.Equal(originalCreatedAt, payment.CreatedAt);
        Assert.Equal(originalUpdatedAt, payment.UpdatedAt);
    }

    [Fact]
    public void PledgePaymentId_has_value_equality_and_generates_nonempty_ids()
    {
        var id = PledgePaymentId.New();

        Assert.NotEqual(Guid.Empty, id.Value);
        Assert.Equal(id, new PledgePaymentId(id.Value));
        Assert.NotEqual(id, PledgePaymentId.New());
    }
}
using ICFAccounting.Domain.Pledgers;
using Microsoft.Extensions.Time.Testing;

namespace ICFAccounting.UnitTests.Domain;

public sealed class PledgerTests
{
    private static readonly DateTimeOffset StartTime = new DateTimeOffset(2026, 1, 15, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Create_with_name_initializes_identity_and_audit_fields()
    {
        var clock = new FakeTimeProvider(StartTime);

        var pledger = Pledger.Create("Community Fund", clock);

        Assert.NotEqual(Guid.Empty, pledger.Id.Value);
        Assert.Equal("Community Fund", pledger.PledgerName);
        Assert.True(pledger.Active);
        Assert.Null(pledger.Email);
        Assert.False(pledger.EnableNotification);
        Assert.Equal(StartTime.UtcDateTime, pledger.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, pledger.CreatedAt.Kind);
        Assert.Equal(pledger.CreatedAt, pledger.UpdatedAt);
    }

    [Fact]
    public void Create_without_clock_uses_current_utc_time()
    {
        var before = DateTime.UtcNow;

        var pledger = Pledger.Create("Community Fund");

        Assert.Equal(DateTimeKind.Utc, pledger.CreatedAt.Kind);
        Assert.InRange(pledger.CreatedAt, before, DateTime.UtcNow);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_with_empty_name_throws(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(() => Pledger.Create(name!));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Update_changes_name_and_active_flag(bool active)
    {
        var clock = new FakeTimeProvider(StartTime);
        var pledger = Pledger.Create("Community Fund", clock);
        clock.Advance(TimeSpan.FromMinutes(5));

        pledger.Update("Education Fund", active, null, false, clock);

        Assert.Equal("Education Fund", pledger.PledgerName);
        Assert.Equal(active, pledger.Active);
        Assert.Null(pledger.Email);
        Assert.False(pledger.EnableNotification);
        Assert.Equal(StartTime.UtcDateTime, pledger.CreatedAt);
        Assert.Equal(StartTime.AddMinutes(5).UtcDateTime, pledger.UpdatedAt);
        Assert.Equal(DateTimeKind.Utc, pledger.UpdatedAt.Kind);
    }

    [Fact]
    public void Create_and_update_store_email_and_notification_preference()
    {
        var clock = new FakeTimeProvider(StartTime);
        var pledger = Pledger.Create(
            "Community Fund", clock, "community@example.org", enableNotification: true);

        clock.Advance(TimeSpan.FromMinutes(5));
        pledger.Update(
            "Education Fund", false, "education@example.org", enableNotification: false, timeProvider: clock);

        Assert.Equal("education@example.org", pledger.Email);
        Assert.False(pledger.EnableNotification);
        Assert.Equal(StartTime.AddMinutes(5).UtcDateTime, pledger.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("name@example.org extra")]
    public void Create_with_invalid_email_throws(string email)
    {
        Assert.Throws<ArgumentException>(() => Pledger.Create("Community Fund", email: email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Update_with_invalid_email_throws_and_preserves_state(string email)
    {
        var pledger = Pledger.Create("Community Fund", email: "community@example.org");
        var originalUpdatedAt = pledger.UpdatedAt;

        Assert.Throws<ArgumentException>(
            () => pledger.Update("Education Fund", false, email: email, enableNotification: true));

        Assert.Equal("Community Fund", pledger.PledgerName);
        Assert.True(pledger.Active);
        Assert.Equal("community@example.org", pledger.Email);
        Assert.False(pledger.EnableNotification);
        Assert.Equal(originalUpdatedAt, pledger.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Update_with_empty_name_throws_and_preserves_state(string? name)
    {
        var clock = new FakeTimeProvider(StartTime);
        var pledger = Pledger.Create("Community Fund", clock);
        var originalUpdatedAt = pledger.UpdatedAt;
        clock.Advance(TimeSpan.FromMinutes(5));

        Assert.ThrowsAny<ArgumentException>(() => pledger.Update(name!, false, null, false, clock));

        Assert.Equal("Community Fund", pledger.PledgerName);
        Assert.True(pledger.Active);
        Assert.Equal(originalUpdatedAt, pledger.UpdatedAt);
    }

    [Fact]
    public void PledgerId_has_value_equality_and_generates_nonempty_ids()
    {
        var pledgerId = PledgerId.New();

        Assert.NotEqual(Guid.Empty, pledgerId.Value);
        Assert.Equal(pledgerId, new PledgerId(pledgerId.Value));
        Assert.NotEqual(pledgerId, PledgerId.New());
    }
}

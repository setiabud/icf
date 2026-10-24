using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Pledges;
using Microsoft.Extensions.Time.Testing;

namespace ICFAccounting.UnitTests.Domain;

public sealed class PledgeTests
{
    private static readonly DateTimeOffset StartTime = new DateTimeOffset(2026, 1, 15, 9, 30, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(2020)]
    [InlineData(2099)]
    public void Create_initializes_identity_references_and_audit_fields(int year)
    {
        var pledgerId = PledgerId.New();
        var categoryId = PledgeCategoryId.New();
        var clock = new FakeTimeProvider(StartTime);

        var pledge = Pledge.Create(year, 123.45m, pledgerId, categoryId, "  Annual pledge  ", clock);

        Assert.NotEqual(Guid.Empty, pledge.Id.Value);
        Assert.Equal(year, pledge.Year);
        Assert.Equal(123.45m, pledge.Amount);
        Assert.Equal(pledgerId, pledge.PledgerId);
        Assert.Equal(categoryId, pledge.PledgeCategoryId);
        Assert.Equal("Annual pledge", pledge.Comment);
        Assert.Equal(DateTimeKind.Utc, pledge.CreatedAt.Kind);
        Assert.Equal(StartTime.UtcDateTime, pledge.CreatedAt);
        Assert.Equal(pledge.CreatedAt, pledge.UpdatedAt);
        Assert.NotEqual(pledge.Id, Pledge.Create(year, 1m, pledgerId, categoryId).Id);
    }

    [Fact]
    public void Create_without_clock_uses_current_utc_time()
    {
        var before = DateTime.UtcNow;

        var pledge = Pledge.Create(2026, 1m, PledgerId.New(), PledgeCategoryId.New());

        Assert.Equal(DateTimeKind.Utc, pledge.CreatedAt.Kind);
        Assert.InRange(pledge.CreatedAt, before, DateTime.UtcNow);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData(" \t\r\n", null)]
    [InlineData("  A pledge  ", "A pledge")]
    public void Create_and_update_normalize_comments(string? comment, string? expected)
    {
        var pledge = Pledge.Create(2026, 1m, PledgerId.New(), PledgeCategoryId.New(), comment);
        Assert.Equal(expected, pledge.Comment);

        pledge.Update(2027, 2m, pledge.PledgerId, pledge.PledgeCategoryId, comment);

        Assert.Equal(expected, pledge.Comment);
    }

    public static TheoryData<int, decimal, PledgerId, PledgeCategoryId, string> InvalidInputs => new()
    {
        { 2019, 1m, PledgerId.New(), PledgeCategoryId.New(), "year" },
        { 2100, 1m, PledgerId.New(), PledgeCategoryId.New(), "year" },
        { 2026, 0m, PledgerId.New(), PledgeCategoryId.New(), "amount" },
        { 2026, -1m, PledgerId.New(), PledgeCategoryId.New(), "amount" },
        { 2026, 1.001m, PledgerId.New(), PledgeCategoryId.New(), "amount" },
        { 2026, 0.001m, PledgerId.New(), PledgeCategoryId.New(), "amount" },
        { 2026, 1m, default, PledgeCategoryId.New(), "pledgerId" },
        { 2026, 1m, PledgerId.New(), default, "pledgeCategoryId" }
    };

    [Theory]
    [MemberData(nameof(InvalidInputs))]
    public void Create_with_invalid_input_throws(
        int year, decimal amount, PledgerId pledgerId, PledgeCategoryId categoryId, string parameter)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(
            () => Pledge.Create(year, amount, pledgerId, categoryId));

        Assert.Equal(parameter, exception.ParamName);
    }

    [Theory]
    [MemberData(nameof(InvalidInputs))]
    public void Update_with_invalid_input_preserves_all_state(
        int year, decimal amount, PledgerId pledgerId, PledgeCategoryId categoryId, string parameter)
    {
        var clock = new FakeTimeProvider(StartTime);
        var pledge = Pledge.Create(2026, 25m, PledgerId.New(), PledgeCategoryId.New(), "Original", clock);
        var originalId = pledge.Id;
        var originalPledgerId = pledge.PledgerId;
        var originalCategoryId = pledge.PledgeCategoryId;
        var originalCreatedAt = pledge.CreatedAt;
        var originalUpdatedAt = pledge.UpdatedAt;
        clock.Advance(TimeSpan.FromMinutes(5));

        var exception = Assert.ThrowsAny<ArgumentException>(
            () => pledge.Update(year, amount, pledgerId, categoryId, "Changed", clock));

        Assert.Equal(parameter, exception.ParamName);
        Assert.Equal(originalId, pledge.Id);
        Assert.Equal(2026, pledge.Year);
        Assert.Equal(25m, pledge.Amount);
        Assert.Equal("Original", pledge.Comment);
        Assert.Equal(originalPledgerId, pledge.PledgerId);
        Assert.Equal(originalCategoryId, pledge.PledgeCategoryId);
        Assert.Equal(originalCreatedAt, pledge.CreatedAt);
        Assert.Equal(originalUpdatedAt, pledge.UpdatedAt);
    }

    [Fact]
    public void Update_changes_fields_and_preserves_identity_and_creation_time()
    {
        var clock = new FakeTimeProvider(StartTime);
        var pledge = Pledge.Create(2026, 25m, PledgerId.New(), PledgeCategoryId.New(), "Original", clock);
        var originalId = pledge.Id;
        var originalCreatedAt = pledge.CreatedAt;
        var pledgerId = PledgerId.New();
        var categoryId = PledgeCategoryId.New();
        clock.Advance(TimeSpan.FromMinutes(5));

        pledge.Update(2027, 99.99m, pledgerId, categoryId, "  Changed  ", clock);

        Assert.Equal(originalId, pledge.Id);
        Assert.Equal(originalCreatedAt, pledge.CreatedAt);
        Assert.Equal(2027, pledge.Year);
        Assert.Equal(99.99m, pledge.Amount);
        Assert.Equal(pledgerId, pledge.PledgerId);
        Assert.Equal(categoryId, pledge.PledgeCategoryId);
        Assert.Equal("Changed", pledge.Comment);
        Assert.Equal(DateTimeKind.Utc, pledge.UpdatedAt.Kind);
        Assert.Equal(StartTime.AddMinutes(5).UtcDateTime, pledge.UpdatedAt);
    }

    [Fact]
    public void Create_accepts_valid_currency_boundaries_without_rounding()
    {
        foreach (var amount in new[] { 0.01m, 1.2300m, decimal.MaxValue })
        {
            var pledge = Pledge.Create(2026, amount, PledgerId.New(), PledgeCategoryId.New());
            Assert.Equal(amount, pledge.Amount);
        }
    }

    [Fact]
    public void PledgeId_has_value_equality_and_generates_nonempty_ids()
    {
        var id = PledgeId.New();

        Assert.NotEqual(Guid.Empty, id.Value);
        Assert.Equal(id, new PledgeId(id.Value));
        Assert.NotEqual(id, PledgeId.New());
    }
}

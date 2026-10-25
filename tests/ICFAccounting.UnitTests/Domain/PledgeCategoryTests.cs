using ICFAccounting.Domain.PledgeCategories;
using Microsoft.Extensions.Time.Testing;

namespace ICFAccounting.UnitTests.Domain;

public sealed class PledgeCategoryTests
{
    private static readonly DateTimeOffset StartTime = new DateTimeOffset(2026, 1, 15, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Create_with_name_initializes_identity_and_audit_fields()
    {
        var clock = new FakeTimeProvider(StartTime);

        var category = PledgeCategory.Create("Education", clock);

        Assert.NotEqual(Guid.Empty, category.Id.Value);
        Assert.Equal("Education", category.CategoryName);
        Assert.True(category.Active);
        Assert.Equal(StartTime.UtcDateTime, category.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, category.CreatedAt.Kind);
        Assert.Equal(category.CreatedAt, category.UpdatedAt);
    }

    [Fact]
    public void Create_without_clock_uses_current_utc_time()
    {
        var before = DateTime.UtcNow;

        var category = PledgeCategory.Create("Education");

        Assert.Equal(DateTimeKind.Utc, category.CreatedAt.Kind);
        Assert.InRange(category.CreatedAt, before, DateTime.UtcNow);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_with_empty_category_name_throws(string? categoryName)
    {
        Assert.ThrowsAny<ArgumentException>(() => PledgeCategory.Create(categoryName!));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Update_changes_category_name_and_active_flag(bool active)
    {
        var clock = new FakeTimeProvider(StartTime);
        var category = PledgeCategory.Create("Education", clock);
        var originalId = category.Id;
        var originalCreatedAt = category.CreatedAt;
        clock.Advance(TimeSpan.FromMinutes(5));

        category.Update("Healthcare", active, clock);

        Assert.Equal(originalId, category.Id);
        Assert.Equal("Healthcare", category.CategoryName);
        Assert.Equal(active, category.Active);
        Assert.Equal(originalCreatedAt, category.CreatedAt);
        Assert.Equal(StartTime.AddMinutes(5).UtcDateTime, category.UpdatedAt);
        Assert.Equal(DateTimeKind.Utc, category.UpdatedAt.Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Update_with_empty_category_name_throws_and_preserves_state(string? categoryName)
    {
        var clock = new FakeTimeProvider(StartTime);
        var category = PledgeCategory.Create("Education", clock);
        var originalUpdatedAt = category.UpdatedAt;
        clock.Advance(TimeSpan.FromMinutes(5));

        Assert.ThrowsAny<ArgumentException>(() => category.Update(categoryName!, false, clock));

        Assert.Equal("Education", category.CategoryName);
        Assert.True(category.Active);
        Assert.Equal(originalUpdatedAt, category.UpdatedAt);
    }

    [Fact]
    public void PledgeCategoryId_has_value_equality_and_generates_nonempty_ids()
    {
        var categoryId = PledgeCategoryId.New();

        Assert.NotEqual(Guid.Empty, categoryId.Value);
        Assert.Equal(categoryId, new PledgeCategoryId(categoryId.Value));
        Assert.NotEqual(categoryId, PledgeCategoryId.New());
    }
}

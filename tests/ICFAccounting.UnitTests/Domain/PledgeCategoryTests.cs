using ICFAccounting.Domain.PledgeCategories;

namespace ICFAccounting.UnitTests.Domain;

public sealed class PledgeCategoryTests
{
    [Fact]
    public void Create_with_name_initializes_identity_and_audit_fields()
    {
        var category = PledgeCategory.Create("Education");

        Assert.NotEqual(Guid.Empty, category.Id.Value);
        Assert.Equal("Education", category.CategoryName);
        Assert.True(category.Active);
        Assert.Equal(DateTimeKind.Utc, category.CreatedAt.Kind);
        Assert.Equal(category.CreatedAt, category.UpdatedAt);
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
        var category = PledgeCategory.Create("Education");
        var originalId = category.Id;
        var originalCreatedAt = category.CreatedAt;

        category.Update("Healthcare", active);

        Assert.Equal(originalId, category.Id);
        Assert.Equal("Healthcare", category.CategoryName);
        Assert.Equal(active, category.Active);
        Assert.Equal(originalCreatedAt, category.CreatedAt);
        Assert.True(category.UpdatedAt >= originalCreatedAt);
        Assert.Equal(DateTimeKind.Utc, category.UpdatedAt.Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Update_with_empty_category_name_throws_and_preserves_state(string? categoryName)
    {
        var category = PledgeCategory.Create("Education");
        var originalUpdatedAt = category.UpdatedAt;

        Assert.ThrowsAny<ArgumentException>(() => category.Update(categoryName!, false));

        Assert.Equal("Education", category.CategoryName);
        Assert.True(category.Active);
        Assert.Equal(originalUpdatedAt, category.UpdatedAt);
    }
}

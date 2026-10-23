using ICFAccounting.Domain.Pledgers;

namespace ICFAccounting.UnitTests.Domain;

public sealed class PledgerTests
{
    [Fact]
    public void Create_with_name_initializes_identity_and_audit_fields()
    {
        var pledger = Pledger.Create("Community Fund");

        Assert.NotEqual(Guid.Empty, pledger.Id.Value);
        Assert.Equal("Community Fund", pledger.PledgerName);
        Assert.True(pledger.Active);
        Assert.Equal(DateTimeKind.Utc, pledger.CreatedAt.Kind);
        Assert.Equal(pledger.CreatedAt, pledger.UpdatedAt);
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
        var pledger = Pledger.Create("Community Fund");

        pledger.Update("Education Fund", active);

        Assert.Equal("Education Fund", pledger.PledgerName);
        Assert.Equal(active, pledger.Active);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Update_with_empty_name_throws_and_preserves_state(string? name)
    {
        var pledger = Pledger.Create("Community Fund");
        var originalUpdatedAt = pledger.UpdatedAt;

        Assert.ThrowsAny<ArgumentException>(() => pledger.Update(name!, false));

        Assert.Equal("Community Fund", pledger.PledgerName);
        Assert.True(pledger.Active);
        Assert.Equal(originalUpdatedAt, pledger.UpdatedAt);
    }
}

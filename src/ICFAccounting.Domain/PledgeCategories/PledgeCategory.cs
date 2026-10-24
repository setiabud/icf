namespace ICFAccounting.Domain.PledgeCategories;

public sealed class PledgeCategory
{
    private PledgeCategory(
        PledgeCategoryId id,
        string categoryName,
        bool active,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Id = id;
        CategoryName = categoryName;
        Active = active;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public PledgeCategoryId Id { get; }

    public string CategoryName { get; private set; }

    public bool Active { get; private set; }

    public DateTime CreatedAt { get; }

    public DateTime UpdatedAt { get; private set; }

    public static PledgeCategory Create(string categoryName, TimeProvider? timeProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryName);

        var createdAt = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        return new PledgeCategory(
            PledgeCategoryId.New(),
            categoryName,
            true,
            createdAt,
            createdAt);
    }

    public void Update(string categoryName, bool active, TimeProvider? timeProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryName);

        CategoryName = categoryName;
        Active = active;
        UpdatedAt = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
    }
}

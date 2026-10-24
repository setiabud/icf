using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Pledgers;

namespace ICFAccounting.Domain.Pledges;

/// <summary>
/// A monetary pledge for a year, referencing a pledger and category by identity.
/// </summary>
public sealed class Pledge
{
    private Pledge(
        PledgeId id,
        int year,
        decimal amount,
        PledgerId pledgerId,
        PledgeCategoryId pledgeCategoryId,
        string? comment,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Id = id;
        Year = year;
        Amount = amount;
        PledgerId = pledgerId;
        PledgeCategoryId = pledgeCategoryId;
        Comment = comment;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public PledgeId Id { get; }

    public int Year { get; private set; }

    public decimal Amount { get; private set; }

    public string? Comment { get; private set; }

    public PledgerId PledgerId { get; private set; }

    public PledgeCategoryId PledgeCategoryId { get; private set; }

    public DateTime CreatedAt { get; }

    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Creates a pledge for 2020-2099 with a positive amount of at most two decimal places.
    /// </summary>
    public static Pledge Create(
        int year,
        decimal amount,
        PledgerId pledgerId,
        PledgeCategoryId pledgeCategoryId,
        string? comment = null,
        TimeProvider? timeProvider = null)
    {
        Validate(year, amount, pledgerId, pledgeCategoryId);

        var createdAt = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        return new Pledge(
            PledgeId.New(), year, amount, pledgerId, pledgeCategoryId,
            NormalizeComment(comment), createdAt, createdAt);
    }

    /// <summary>
    /// Revalidates and replaces pledge details while preserving identity and creation time.
    /// </summary>
    public void Update(
        int year,
        decimal amount,
        PledgerId pledgerId,
        PledgeCategoryId pledgeCategoryId,
        string? comment = null,
        TimeProvider? timeProvider = null)
    {
        Validate(year, amount, pledgerId, pledgeCategoryId);

        Year = year;
        Amount = amount;
        PledgerId = pledgerId;
        PledgeCategoryId = pledgeCategoryId;
        Comment = NormalizeComment(comment);
        UpdatedAt = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
    }

    private static void Validate(
        int year, decimal amount, PledgerId pledgerId, PledgeCategoryId pledgeCategoryId)
    {
        if (year is < 2020 or > 2099)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be between 2020 and 2099.");
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new ArgumentException("Amount must have at most two decimal places.", nameof(amount));
        }

        if (pledgerId == default)
        {
            throw new ArgumentException("Pledger ID cannot be empty.", nameof(pledgerId));
        }

        if (pledgeCategoryId == default)
        {
            throw new ArgumentException("Pledge category ID cannot be empty.", nameof(pledgeCategoryId));
        }
    }

    private static string? NormalizeComment(string? comment)
    {
        return string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
    }
}

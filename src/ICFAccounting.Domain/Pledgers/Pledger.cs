namespace ICFAccounting.Domain.Pledgers;

public sealed class Pledger
{
    private Pledger(
        PledgerId id,
        string pledgerName,
        bool active,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Id = id;
        PledgerName = pledgerName;
        Active = active;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public PledgerId Id { get; }

    public string PledgerName { get; private set; }

    public bool Active { get; private set; }

    public DateTime CreatedAt { get; }

    public DateTime UpdatedAt { get; private set; }

    public static Pledger Create(string pledgerName, TimeProvider? timeProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pledgerName);

        var createdAt = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        return new Pledger(PledgerId.New(), pledgerName, true, createdAt, createdAt);
    }

    public void Update(string pledgerName, bool active, TimeProvider? timeProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pledgerName);

        PledgerName = pledgerName;
        Active = active;
        UpdatedAt = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
    }
}

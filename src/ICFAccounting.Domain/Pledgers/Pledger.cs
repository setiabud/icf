namespace ICFAccounting.Domain.Pledgers;

public sealed class Pledger
{
    private Pledger(
        PledgerId id,
        string name,
        bool active,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Id = id;
        Name = name;
        Active = active;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public PledgerId Id { get; }

    public string Name { get; private set; }

    public bool Active { get; private set; }

    public DateTime CreatedAt { get; }

    public DateTime UpdatedAt { get; private set; }

    public static Pledger Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var createdAt = DateTime.UtcNow;
        return new Pledger(PledgerId.New(), name, true, createdAt, createdAt);
    }

    public void Update(string name, bool active)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Active = active;
        UpdatedAt = DateTime.UtcNow;
    }
}

using System.Net.Mail;

namespace ICFAccounting.Domain.Pledgers;

public sealed class Pledger
{
    private Pledger(
        PledgerId id,
        string pledgerName,
        bool active,
        string? email,
        bool enableNotification,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Id = id;
        PledgerName = pledgerName;
        Active = active;
        Email = email;
        EnableNotification = enableNotification;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public PledgerId Id { get; }

    public string PledgerName { get; private set; }

    public bool Active { get; private set; }

    public string? Email { get; private set; }

    public bool EnableNotification { get; private set; }

    public DateTime CreatedAt { get; }

    public DateTime UpdatedAt { get; private set; }

    public static Pledger Create(
        string pledgerName,
        TimeProvider? timeProvider = null,
        string? email = null,
        bool enableNotification = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pledgerName);
        ValidateEmail(email);

        var createdAt = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        return new Pledger(PledgerId.New(), pledgerName, true, email, enableNotification, createdAt, createdAt);
    }

    public void Update(
        string pledgerName,
        bool active,
        string? email,
        bool enableNotification,
        TimeProvider? timeProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pledgerName);
        ValidateEmail(email);

        PledgerName = pledgerName;
        Active = active;
        Email = email;
        EnableNotification = enableNotification;
        UpdatedAt = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
    }

    private static void ValidateEmail(string? email)
    {
        if (email is null)
        {
            return;
        }

        if (!MailAddress.TryCreate(email, out var address)
            || !string.Equals(address.Address, email, StringComparison.Ordinal))
        {
            throw new ArgumentException("Email must be a valid email address.", nameof(email));
        }
    }
}

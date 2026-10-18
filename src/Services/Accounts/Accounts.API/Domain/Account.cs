namespace Accounts.API.Domain;

public sealed class Account
{
    private const int CURRENCY_LENGTH = 3;

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Currency { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Account() { }

    public Account(Guid id, Guid userId, string currency)
    {
        if (currency.Length != CURRENCY_LENGTH || !currency.All(char.IsLetter))
            throw new ArgumentException($"Currency must be {CURRENCY_LENGTH} characters long and contain only letters.", nameof(currency));

        Id = id;
        UserId = userId;
        Currency = currency.ToUpperInvariant();
        CreatedAt = DateTime.UtcNow;
    }
}
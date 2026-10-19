namespace Accounts.API.Domain;

public sealed class Account
{
    private const int CURRENCY_LENGTH = 3;

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Currency { get; private set; }
    public decimal Balance { get; private set; }
    public decimal Hold { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Account() { }

    public Account(Guid id, Guid userId, string currency)
    {
        if (currency.Length != CURRENCY_LENGTH || !currency.All(char.IsLetter))
            throw new ArgumentException($"Currency must be {CURRENCY_LENGTH} characters long and contain only letters.", nameof(currency));

        Id = id;
        UserId = userId;
        Currency = currency.ToUpperInvariant();
        Balance = 0;
        Hold = 0;
        CreatedAt = DateTime.UtcNow;
    }

    public void TopUpBalance(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        Balance += amount;
    }

    public void Reserve(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");
        if (Balance < amount)
            throw new InvalidOperationException("Insufficient funds.");

        Balance -= amount;
        Hold += amount;
    }

    public void Commit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");
        if (Hold < amount)
            throw new InvalidOperationException("Hold too small.");

        Hold -= amount;
    }

    public void Release(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");
        if (Hold < amount)
            throw new InvalidOperationException("Hold too small.");

        Hold -= amount;
        Balance += amount;
    }
}
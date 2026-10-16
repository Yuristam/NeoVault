using Accounts.API.Enums;

namespace Accounts.API.Entities;

public sealed class LedgerEntry
{
    public long Id { get; private set; }
    public Guid TransactionId { get; private set; }
    public Guid AccountId { get; private set; }
    public long Amount { get; private set; }
    public EntryDirection Direction { get; private set; }
    public long BalanceAfter { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private LedgerEntry() { }
}
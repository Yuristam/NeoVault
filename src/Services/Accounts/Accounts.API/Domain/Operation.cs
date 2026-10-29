namespace Accounts.API.Domain
{
    public class Operation
    {
        public string Id { get; private set; } = default!;
        public Guid AccountId { get; private set; }
        public string Kind { get; private set; } = default!;
        public decimal Amount { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private Operation()
        {
            
        }

        public Operation(string id, Guid accountId, string kind, decimal amount)
        {
            Id = id;
            AccountId = accountId;
            Kind = kind;
            Amount = amount;
            CreatedAt = DateTime.UtcNow;
        }
    }
}

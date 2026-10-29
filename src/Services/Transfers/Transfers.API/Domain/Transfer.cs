namespace Transfers.API.Domain
{
    public class Transfer
    {
        public Guid Id { get; private set; }
        public Guid FromAccountId { get; private set; }
        public Guid ToAccountId { get; private set; }
        public decimal Amount { get; private set; }
        public TransferStatus Status { get; set; }
        public DateTime CreatedAt { get; private set; }

        private Transfer()
        {    
        }

        public Transfer(Guid id, Guid fromAccountId, Guid toAccountId, decimal amount, TransferStatus status)
        {
            Id = id;
            FromAccountId = fromAccountId;
            ToAccountId = toAccountId;

            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.");
            
            Amount = amount;
            Status = status;
            CreatedAt = DateTime.UtcNow;
        }
    }
}

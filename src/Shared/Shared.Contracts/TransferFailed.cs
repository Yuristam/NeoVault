namespace Shared.Contracts
{
    public record TransferFailed(
        Guid TransferId,
        Guid FromAccountId,
        Guid ToAccountId,
        decimal Amount);
}

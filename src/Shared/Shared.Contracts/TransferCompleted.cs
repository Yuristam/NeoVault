namespace Shared.Contracts;

public record TransferCompleted(
    Guid TransferId,
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount);

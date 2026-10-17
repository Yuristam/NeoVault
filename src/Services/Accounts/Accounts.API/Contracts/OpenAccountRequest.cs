namespace Accounts.API.Contracts;

public record OpenAccountRequest(Guid UserId, string Currency);
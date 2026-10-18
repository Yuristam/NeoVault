using Accounts.API.Contracts;
using Accounts.API.Data;
using Accounts.API.Domain;
using Accounts.API.Enums;
using Microsoft.EntityFrameworkCore;

namespace Accounts.API.Endpoints
{
    public static class AccountEndpoints
    {
        public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/accounts");

            // POST /accounts — открыть счёт
            group.MapPost("/", async (OpenAccountRequest req, AccountsDbContext db) =>
            {
                var account = new Account(Guid.NewGuid(), req.UserId, req.Currency);
                db.Accounts.Add(account);
                await db.SaveChangesAsync();
                return Results.Created($"/accounts/{account.Id}", new { account.Id });
            });

            // POST /accounts/{id}/top-up — пополнение (первая запись в ledger!)
            group.MapPost("/{id:guid}/top-up", async (Guid id, TopUpRequest req, AccountsDbContext db) =>
            {
                await using var tx = await db.Database.BeginTransactionAsync();

                var account = await db.Accounts.FindAsync(id);
                if (account is null) return Results.NotFound();

                var balance = await db.LedgerEntries
                    .Where(e => e.AccountId == id)
                    .SumAsync(e => (long?)e.Direction * e.Amount) ?? 0;

                var entry = new LedgerEntry(Guid.NewGuid(), id, req.Amount, EntryDirection.Credit, balance + req.Amount);
                db.LedgerEntries.Add(entry);
                await db.SaveChangesAsync();
                await tx.CommitAsync();

                return Results.Ok(new { balance = balance + req.Amount });
            });

            // GET /accounts/{id}/balance — баланс = SUM записей, а не поле!
            group.MapGet("/{id:guid}/balance", async (Guid id, AccountsDbContext db) =>
            {
                var exists = await db.Accounts.AnyAsync(a => a.Id == id);
                if (!exists) return Results.NotFound();

                var balance = await db.LedgerEntries
                    .Where(e => e.AccountId == id)
                    .SumAsync(e => (long?)e.Direction * e.Amount) ?? 0;

                return Results.Ok(new { balance });
            });


            return app;
        }
    }
}

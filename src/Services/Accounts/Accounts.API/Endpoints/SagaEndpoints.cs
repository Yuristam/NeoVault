using Accounts.API.Data;
using Accounts.API.Domain;

namespace Accounts.API.Endpoints
{
    public static class SagaEndpoints
    {
        public static IEndpointRouteBuilder MapSagaEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/accounts/{id:guid}");

            // reserve: замок на деньги (balance -> hold)
            group.MapPost("/reserve", (Guid id, decimal amount, AccountsDbContext db) =>
                RunAccountOperation(id, amount, db, (acc, a) => acc.Reserve(a)));

            // credit: зачисление получателю
            group.MapPost("/credit", (Guid id, decimal amount, AccountsDbContext db) =>
                RunAccountOperation(id, amount, db, (acc, a) => acc.TopUpBalance(a)));

            // commit: финализация, необратимо (деньги в полёте доставлены)
            group.MapPost("/commit", (Guid id, decimal amount, AccountsDbContext db) =>
                RunAccountOperation(id, amount, db, (acc, a) => acc.Commit(a)));

            // release: компенсация (замок снят, деньги вернулись)
            group.MapPost("/release", (Guid id, decimal amount, AccountsDbContext db) =>
                RunAccountOperation(id, amount, db, (acc, a) => acc.Release(a)));

            return app;
        }

        // общая сантехника:
        private static async Task<IResult> RunAccountOperation(
            Guid id, decimal amount, AccountsDbContext db, Action<Account, decimal> operation)
        {
            var account = await db.Accounts.FindAsync(id);
            if (account is null) return Results.NotFound();

            try
            {
                operation(account, amount);
            }
            catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }

            await db.SaveChangesAsync();
            return Results.Ok(account);
        }
    }
}

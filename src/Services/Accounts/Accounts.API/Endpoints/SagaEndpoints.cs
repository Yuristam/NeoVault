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
            group.MapPost("/reserve", async (Guid id, decimal amount, AccountsDbContext db) =>
            {
                var account = await db.Accounts.FindAsync(id);
                if (account is null) return Results.NotFound();

                try
                {
                    account.Reserve(amount);
                }
                catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
                catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }

                await db.SaveChangesAsync();
                return Results.Ok(account);
            });

            // credit: зачисление получателю
            group.MapPost("/credit", async (Guid id, decimal amount, AccountsDbContext db) =>
            {
                var account = await db.Accounts.FindAsync(id);
                if (account is null) return Results.NotFound();

                try
                {
                    account.TopUpBalance(amount); // TODO: в будущем заменить на Credit метод в Account - домене
                }
                catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
                catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }

                await db.SaveChangesAsync();
                return Results.Ok(account);
            });

            // commit: финализация, необратимо (деньги в полёте доставлены)
            group.MapPost("/commit", async(Guid id, decimal amount, AccountsDbContext db) =>
            {
                var account = await db.Accounts.FindAsync(id);
                if (account is null) return Results.NotFound();

                try
                {
                    account.Commit(amount);
                }
                catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
                catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }

                await db.SaveChangesAsync();
                return Results.Ok(account);
            });

            // release: компенсация (замок снят, деньги вернулись)
            group.MapPost("/release", async(Guid id, decimal amount, AccountsDbContext db) =>
            {
                var account = await db.Accounts.FindAsync(id);
                if (account is null) return Results.NotFound();

                try
                {
                    account.Release(amount);
                }
                catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
                catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }

                await db.SaveChangesAsync();
                return Results.Ok(account);
            });

            return app;
        }
    }
}

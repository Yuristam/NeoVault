using Accounts.API.Data;
using Accounts.API.Domain;
using Microsoft.EntityFrameworkCore;

namespace Accounts.API.Endpoints
{
    public static class AccountEndpoints
    {
        public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/accounts");

            // GET: Get all accounts 
            group.MapGet("/", async (AccountsDbContext db) =>
                Results.Ok(await db.Accounts.ToListAsync()));

            // GET: Get account by id
            group.MapGet("/{id}", async (Guid id, AccountsDbContext db) =>
            {
                var account = await db.Accounts.FindAsync(id);
                
                return account == null 
                    ? Results.NotFound() 
                    : Results.Ok();
            });

            // POST: Create account
            group.MapPost("/create-account", async (string currency, AccountsDbContext db) =>
            {
                var account = new Account(Guid.NewGuid(), Guid.NewGuid(), currency);
                
                db.Accounts.Add(account);
                await db.SaveChangesAsync();

                return Results.Ok(account);
            });
            
            // DELETE: Delete account
            group.MapDelete("/delete-account", async (Guid id, AccountsDbContext db) =>
            {
                var account = await db.Accounts.FindAsync(id);
                
                if (account == null) return Results.NotFound();

                db.Accounts.Remove(account);
                await db.SaveChangesAsync();

                return Results.NoContent();
            });

            return app;
        }
    }
}

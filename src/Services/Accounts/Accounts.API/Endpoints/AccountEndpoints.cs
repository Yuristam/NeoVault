using Accounts.API.Domain;
using Accounts.API.Persistence;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;

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
            group.MapGet("/{id}", async (Guid id, AccountsDbContext db, IConnectionMultiplexer redis) =>
            {
                var cacheKey = $"account:{id}";
                var redisDatabase = redis.GetDatabase();

                var cachedJson = await redisDatabase.StringGetAsync(cacheKey);
                if (cachedJson.HasValue)
                {
                    Console.WriteLine("GET: из кеша Redis");
                    var cachedAccount = JsonSerializer.Deserialize<Account>(cachedJson!);
                    return Results.Ok(cachedAccount);
                }

                var account = await db.Accounts.FindAsync(id);
                if (account == null) return Results.NotFound();
                
                Console.WriteLine("GET: из базы");

                await redisDatabase.StringSetAsync(
                    cacheKey,
                    JsonSerializer.Serialize(account),
                    TimeSpan.FromSeconds(60));
                
                return Results.Ok(account);
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

            // POST: Top-up balance
            group.MapPost("/{id}/top-up", async (Guid id, decimal amount, AccountsDbContext db) =>
            {
                var account = await db.Accounts.FindAsync(id);
                if (account == null) return Results.NotFound();

                account.TopUpBalance(amount);
                await db.SaveChangesAsync();

                return Results.Ok(account);
            });

            return app;
        }
    }
}

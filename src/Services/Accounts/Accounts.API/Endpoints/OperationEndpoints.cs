using Accounts.API.Persistence;

namespace Accounts.API.Endpoints
{
    public static class OperationEndpoints
    {
        public static IEndpointRouteBuilder MapOperationEnpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/operations/{key}", async (string key, AccountsDbContext db) =>
            {
                var op = await db.Operations.FindAsync(key);
                return op is null ? Results.NotFound() : Results.Ok(op);
            });

            return app;
        }
    }
}

using Accounts.API.Persistence;

namespace Accounts.API.Endpoints
{
    public static class OperationEndpoints
    {
        public static IEndpointRouteBuilder MapOperationEnpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/operations/{key}", async (string key, AccountsDbContext dbContext) =>
            {
                var operation = await dbContext.Operations.FindAsync(key);

                return operation is null 
                    ? Results.NotFound() 
                    : Results.Ok(operation);
            });

            return app;
        }
    }
}

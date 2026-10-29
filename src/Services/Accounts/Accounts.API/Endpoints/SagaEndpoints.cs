using Accounts.API.Domain;
using Accounts.API.Persistence;

namespace Accounts.API.Endpoints
{
    public static class SagaEndpoints
    {
        public static IEndpointRouteBuilder MapSagaEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/accounts/{id:guid}");

            // reserve: замок на деньги (balance -> hold)
            group.MapPost("/reserve", async (Guid id, decimal amount, Guid? transferId, AccountsDbContext dbContext) =>
            {
                var account = await dbContext.Accounts.FindAsync(id);

                if (account is null) return Results.NotFound();

                var operationKey = transferId is null 
                    ? null 
                    : $"transfer:{transferId}:reserve";

                try
                {
                    // чек с таким номером уже пробит? — не делаем дважды, просто отвечаем как в первый раз
                    if (operationKey != null && await dbContext.Operations.FindAsync(operationKey) is not null)
                        return Results.Ok(account);

                    account.Reserve(amount);

                    if (operationKey != null)
                        dbContext.Operations.Add(new Operation(operationKey, id, "reserve", amount));

                    await dbContext.SaveChangesAsync(); // и деньги, и чек — одной транзакцией: либо оба, либо ничего
                    return Results.Ok(account);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(new { error = ex.Message });
                }
            });

            // credit: зачисление получателю
            group.MapPost("/credit", async (Guid id, decimal amount, Guid? transferId, AccountsDbContext dbContext) => 
            {
                var account = await dbContext.Accounts.FindAsync(id);

                if (account is null) return Results.NotFound();

                var operationKey = transferId is null 
                    ? null 
                    : $"transfer:{transferId}:credit";

                try
                {
                    // чек с таким номером уже пробит? — не делаем дважды, просто отвечаем как в первый раз
                    if (operationKey != null && await dbContext.Operations.FindAsync(operationKey) is not null)
                        return Results.Ok(account);

                    // await Task.Delay(5000); // имитация долгого перевода

                    account.TopUpBalance(amount);

                    if (operationKey != null)
                        dbContext.Operations.Add(new Operation(operationKey, id, "credit", amount));

                    await dbContext.SaveChangesAsync(); // и деньги, и чек — одной транзакцией: либо оба, либо ничего
                    return Results.Ok(account);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(new { error = ex.Message });
                }
            });

            // commit: финализация, необратимо (деньги в полёте доставлены)
            group.MapPost("/commit", async (Guid id, decimal amount, Guid? transferId, AccountsDbContext dbContext) => 
            {
                var account = await dbContext.Accounts.FindAsync(id);

                if (account is null) return Results.NotFound();

                var operationKey = transferId is null 
                    ? null 
                    : $"transfer:{transferId}:commit";

                try
                {
                    // чек с таким номером уже пробит? — не делаем дважды, просто отвечаем как в первый раз
                    if (operationKey != null && await dbContext.Operations.FindAsync(operationKey) is not null)
                        return Results.Ok(account);

                    account.Commit(amount);
                    
                    if (operationKey != null)
                        dbContext.Operations.Add(new Operation(operationKey, id, "commit", amount));

                    await dbContext.SaveChangesAsync(); // и деньги, и чек — одной транзакцией: либо оба, либо ничего
                    return Results.Ok(account);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(new { error = ex.Message });
                }
            });

            // release: компенсация (замок снят, деньги вернулись)
            group.MapPost("/release", async (Guid id, decimal amount, Guid? transferId, AccountsDbContext dbContext) =>
            {
                var account = await dbContext.Accounts.FindAsync(id);
                
                if (account is null) return Results.NotFound();

                var operationKey = transferId is null 
                    ? null 
                    : $"transfer:{transferId}:release";

                try
                {
                    // чек с таким номером уже пробит? — не делаем дважды, просто отвечаем как в первый раз
                    if (operationKey != null && await dbContext.Operations.FindAsync(operationKey) is not null)
                        return Results.Ok(account);

                    account.Release(amount);
                    
                    if (operationKey != null)
                        dbContext.Operations.Add(new Operation(operationKey, id, "release", amount));

                    await dbContext.SaveChangesAsync(); // и деньги, и чек — одной транзакцией: либо оба, либо ничего
                    return Results.Ok(account);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(new { error = ex.Message });
                }
            });

            return app;
        }
    }
}

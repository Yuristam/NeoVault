using MassTransit;
using Shared.Contracts;
using Transfers.API.Domain;
using Transfers.API.Enums;
using Transfers.API.Integration;
using Transfers.API.Persistence;

namespace Transfers.API.Endpoints
{
    public static class TransferEndpoints
    {   
        public static IEndpointRouteBuilder MapTransferEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/transfers/{id}", async (Guid transferId, TransfersDbContext dbContext) =>
            {
                var transfer = await dbContext.Transfers.FindAsync(transferId);
                return transfer is null ? Results.NotFound() : Results.Ok(transfer);
            });

            // POST: /transfers?from=<guid>&to=<guid>&amount=50
            app.MapPost("/transfers", async (Guid fromAccount, Guid toAccount, decimal amount, 
                TransfersDbContext dbContext, IHttpClientFactory factory, IPublishEndpoint publisher) =>
            {
                var transfer = new Transfer(Guid.NewGuid(), fromAccount, toAccount, amount, TransferStatus.Pending);
                
                dbContext.Transfers.Add(transfer);
                await dbContext.SaveChangesAsync();

                var client = factory.CreateClient("accounts");
                var amountStr = AccountsApiClient.FormatAmount(amount);

                // шаг 1: замок на деньги отправителя
                var reserveStep = await AccountsApiClient.PostStepAsync(client,
                    $"/accounts/{fromAccount}/reserve?amount={amountStr}&transferId={transfer.Id}");
                
                if (reserveStep != StepResult.Ok)
                    return await CloseTransferAsync(dbContext, transfer, reserveStep == StepResult.Refused 
                        ? TransferStatus.Failed 
                        : TransferStatus.Unknown, $"reserve: {reserveStep}");

                // шаг 2: зачисление получателю
                var creditStep = await AccountsApiClient.PostStepAsync(client,
                    $"/accounts/{toAccount}/credit?amount={amountStr}&transferId={transfer.Id}");
                
                if (creditStep != StepResult.Ok)
                {
                    if (creditStep == StepResult.Refused)
                    {
                        // единственное место во всей саге, где release безопасен: точно знаем, что деньги не ушли
                        await AccountsApiClient.PostStepAsync(client,
                            $"/accounts/{fromAccount}/release?amount={amountStr}&transferId={transfer.Id}");
                    }
                    return await CloseTransferAsync(dbContext, transfer,
                        creditStep == StepResult.Refused ? TransferStatus.Failed : TransferStatus.Unknown,
                        $"credit: {creditStep}");
                }

                // шаг 3: финализация (бухгалтерия — экономически перевод уже произошёл)
                var commitStep = await AccountsApiClient.PostStepAsync(client,
                    $"/accounts/{fromAccount}/commit?amount={amountStr}&transferId={transfer.Id}");
                
                if (commitStep != StepResult.Ok)
                    return await CloseTransferAsync(dbContext, transfer, TransferStatus.Unknown, $"commit: {commitStep}");

                if (transfer.Status == TransferStatus.Completed)
                    await publisher.Publish(new TransferCompleted(transfer.Id, transfer.FromAccountId, transfer.ToAccountId, transfer.Amount));
                else if (transfer.Status == TransferStatus.Failed)
                    await publisher.Publish(new TransferFailed(transfer.Id, transfer.FromAccountId, transfer.ToAccountId, transfer.Amount));
                

                await dbContext.SaveChangesAsync();
                return Results.Ok(transfer);
            });

            app.MapPut("/transfers/{id}", async (Guid id, Guid fromAccount, Guid toAccount, decimal amount, 
                TransfersDbContext dbContext, IHttpClientFactory factory, IPublishEndpoint publisher) =>
            {
                var existing = await dbContext.Transfers.FindAsync(id);
                
                if (existing is not null)
                    return Results.Ok(existing);

                var transfer = new Transfer(id, fromAccount, toAccount, amount, TransferStatus.Pending);
                
                dbContext.Transfers.Add(transfer);
                await dbContext.SaveChangesAsync();

                var client = factory.CreateClient("accounts");
                var amountStr = AccountsApiClient.FormatAmount(amount);

                // шаг 1: замок на деньги отправителя
                var reserveStep = await AccountsApiClient.PostStepAsync(client,
                    $"/accounts/{fromAccount}/reserve?amount={amountStr}&transferId={transfer.Id}");

                if (reserveStep != StepResult.Ok)
                    return await CloseTransferAsync(dbContext, transfer, reserveStep == StepResult.Refused 
                        ? TransferStatus.Failed 
                        : TransferStatus.Unknown, $"reserve: {reserveStep}");


                // шаг 2: зачисление получателю
                var creditStep = await AccountsApiClient.PostStepAsync(client,
                    $"/accounts/{toAccount}/credit?amount={amountStr}&transferId={transfer.Id}");

                if (creditStep != StepResult.Ok)
                {
                    if (creditStep == StepResult.Refused)
                    {
                        // единственное место во всей саге, где release безопасен: точно знаем, что деньги не ушли
                        await AccountsApiClient.PostStepAsync(client,
                            $"/accounts/{fromAccount}/release?amount={amountStr}&transferId={transfer.Id}");
                    }

                    return await CloseTransferAsync(dbContext, transfer, creditStep == StepResult.Refused 
                        ? TransferStatus.Failed 
                        : TransferStatus.Unknown, $"credit: {creditStep}");
                }

                // шаг 3: финализация (бухгалтерия — экономически перевод уже произошёл)
                var commitStep = await AccountsApiClient.PostStepAsync(client,
                    $"/accounts/{fromAccount}/commit?amount={amountStr}&transferId={transfer.Id}");

                if (commitStep != StepResult.Ok)
                    return await CloseTransferAsync(dbContext, transfer, TransferStatus.Unknown, $"commit: {commitStep}");

                transfer.Status = TransferStatus.Completed;
                
                await dbContext.SaveChangesAsync();
                await publisher.Publish(new TransferCompleted(transfer.Id, transfer.FromAccountId, transfer.ToAccountId, transfer.Amount));

                return Results.Ok(transfer);
            });

            return app;
        }

        private static async Task<IResult> CloseTransferAsync(TransfersDbContext dbContext, Transfer transfer, TransferStatus status, string reason)
        {
            transfer.Status = status;

            await dbContext.SaveChangesAsync();
            return Results.Conflict(new { transfer.Id, transfer.Status, reason });
        }
    }
}

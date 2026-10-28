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
            // POST: /transfers?from=<guid>&to=<guid>&amount=50
            app.MapPost("/transfers", async (Guid from, Guid to, decimal amount, TransfersDbContext db, IHttpClientFactory factory, IPublishEndpoint publisher) =>
            {
                var transfer = new Transfer(Guid.NewGuid(), from, to, amount, TransferStatus.Pending);
                db.Transfers.Add(transfer);
                await db.SaveChangesAsync();

                var client = factory.CreateClient("accounts");
                var amountStr = AccountsApiClient.FormatAmount(amount);

                // шаг 1: замок на деньги отправителя
                var reserveStep = await AccountsApiClient.PostStepAsync(client,
                    $"/accounts/{from}/reserve?amount={amountStr}&transferId={transfer.Id}");
                
                if (reserveStep != StepResult.Ok)
                    return await CloseTransferAsync(db, transfer, 
                        reserveStep == StepResult.Refused ? TransferStatus.Failed : TransferStatus.Unknown, $"reserve: {reserveStep}");


                // шаг 2: зачисление получателю
                var creditStep = await AccountsApiClient.PostStepAsync(client,
                    $"/accounts/{to}/credit?amount={amountStr}&transferId={transfer.Id}");
                
                if (creditStep != StepResult.Ok)
                {
                    if (creditStep == StepResult.Refused)
                    {
                        // единственное место во всей саге, где release безопасен: точно знаем, что деньги не ушли
                        await AccountsApiClient.PostStepAsync(client,
                            $"/accounts/{from}/release?amount={amountStr}&transferId={transfer.Id}");
                    }
                    return await CloseTransferAsync(db, transfer,
                        creditStep == StepResult.Refused ? TransferStatus.Failed : TransferStatus.Unknown,
                        $"credit: {creditStep}");
                }

                // шаг 3: финализация (бухгалтерия — экономически перевод уже произошёл)
                var commitStep = await AccountsApiClient.PostStepAsync(client,
                    $"/accounts/{from}/commit?amount={amountStr}&transferId={transfer.Id}");
                
                if (commitStep != StepResult.Ok)
                    return await CloseTransferAsync(db, transfer, TransferStatus.Unknown, $"commit: {commitStep}");

                transfer.Status = TransferStatus.Completed;

                await publisher.Publish(new TransferCompleted(
                    transfer.Id, 
                    transfer.FromAccountId, 
                    transfer.ToAccountId, 
                    transfer.Amount));

                await db.SaveChangesAsync();
                return Results.Ok(transfer);
            });

            app.MapPut("/transfers/{id}", async (Guid id, Guid from, Guid to, decimal amount, TransfersDbContext db, IHttpClientFactory factory, IPublishEndpoint publisher) =>
            {
                var existing = await db.Transfers.FindAsync(id);
                if (existing is not null)
                {
                    return Results.Ok(existing);
                }

                var transfer = new Transfer(id, from, to, amount, TransferStatus.Pending);
                db.Transfers.Add(transfer);
                await db.SaveChangesAsync();

                var client = factory.CreateClient("accounts");
                var amountStr = AccountsApiClient.FormatAmount(amount);

                // шаг 1: замок на деньги отправителя
                var reserveStep = await AccountsApiClient.PostStepAsync(client,
                    $"/accounts/{from}/reserve?amount={amountStr}&transferId={transfer.Id}");

                if (reserveStep != StepResult.Ok)
                    return await CloseTransferAsync(db, transfer,
                        reserveStep == StepResult.Refused ? TransferStatus.Failed : TransferStatus.Unknown, $"reserve: {reserveStep}");


                // шаг 2: зачисление получателю
                var creditStep = await AccountsApiClient.PostStepAsync(client,
                    $"/accounts/{to}/credit?amount={amountStr}&transferId={transfer.Id}");

                if (creditStep != StepResult.Ok)
                {
                    if (creditStep == StepResult.Refused)
                    {
                        // единственное место во всей саге, где release безопасен: точно знаем, что деньги не ушли
                        await AccountsApiClient.PostStepAsync(client,
                            $"/accounts/{from}/release?amount={amountStr}&transferId={transfer.Id}");
                    }
                    return await CloseTransferAsync(db, transfer,
                        creditStep == StepResult.Refused ? TransferStatus.Failed : TransferStatus.Unknown,
                        $"credit: {creditStep}");
                }

                // шаг 3: финализация (бухгалтерия — экономически перевод уже произошёл)
                var commitStep = await AccountsApiClient.PostStepAsync(client,
                    $"/accounts/{from}/commit?amount={amountStr}&transferId={transfer.Id}");

                if (commitStep != StepResult.Ok)
                    return await CloseTransferAsync(db, transfer, TransferStatus.Unknown, $"commit: {commitStep}");

                transfer.Status = TransferStatus.Completed;
                await db.SaveChangesAsync();

                await publisher.Publish(new TransferCompleted(transfer.Id, transfer.FromAccountId, transfer.ToAccountId, transfer.Amount));

                return Results.Ok(transfer);
            });

            return app;
        }

        private static async Task<IResult> CloseTransferAsync(TransfersDbContext db, Transfer t, TransferStatus status, string reason)
        {
            t.Status = status;
            await db.SaveChangesAsync();
            return Results.Conflict(new { t.Id, t.Status, reason });
        }
    }
}

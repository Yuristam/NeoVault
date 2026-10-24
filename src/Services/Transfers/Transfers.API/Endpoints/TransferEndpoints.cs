using System.Globalization;
using Transfers.API.Domain;
using Transfers.API.Enums;
using Transfers.API.Persistence;

namespace Transfers.API.Endpoints
{
    public static class TransferEndpoints
    {
        // culture-независимое число в query (иначе "50,5" сломает запрос на ru-локали)
        private static string Q(decimal amount) => amount.ToString(CultureInfo.InvariantCulture);
        
        private static async Task<StepResult> CallAsync(HttpClient client, string url)
        {
            try
            {
                var response = await client.PostAsync(url, null);
                return response.IsSuccessStatusCode ? StepResult.Ok : StepResult.Refused;
            }
            catch
            {
                return StepResult.Unknown; // тишина любого сорта = не знаем
            }
        }
        
        public static IEndpointRouteBuilder MapTransferEndpoints(this IEndpointRouteBuilder app)
        {
            // POST: /transfers?from=<guid>&to=<guid>&amount=50
            app.MapPost("/transfers", async (Guid from, Guid to, decimal amount, TransfersDbContext db, IHttpClientFactory factory) =>
            {
                var transfer = new Transfer(Guid.NewGuid(), from, to, amount, TransferStatus.Pending);
                db.Transfers.Add(transfer);
                await db.SaveChangesAsync(); // запись о переводе живёт ДО первого шага

                var client = factory.CreateClient("accounts");

                // шаг 1: замок на деньги отправителя
                var reserve = await CallAsync(client, $"/accounts/{from}/reserve?amount={Q(amount)}");
                if (reserve != StepResult.Ok)
                    return await End(db, transfer,
                        reserve == StepResult.Refused ? TransferStatus.Failed : TransferStatus.Unknown,
                        $"reserve: {reserve}");

                // шаг 2: зачисление получателю
                var credit = await CallAsync(client, $"/accounts/{to}/credit?amount={Q(amount)}");
                if (credit != StepResult.Ok)
                {
                    if(credit == StepResult.Refused)
                        await CallAsync(client, $"/accounts/{from}/release?amount={Q(amount)}"); // компенсация
                    
                    return await End(db, transfer, credit == StepResult.Refused ? TransferStatus.Failed : TransferStatus.Unknown,
                        $"credit {credit}");
                }

                // шаг 3: финализация
                var commit = await CallAsync(client, $"/accounts/{from}/commit?amount={Q(amount)}");
                if (commit != StepResult.Ok) 
                    return await End(db, transfer, TransferStatus.Unknown, $"commit: {commit}");

                transfer.Status = TransferStatus.Completed;
                await db.SaveChangesAsync();
                return Results.Ok(transfer);
            });

            return app;
        }

        private static async Task<IResult> End(TransfersDbContext db, Transfer t, TransferStatus status,string reason)
        {
            t.Status = status;

            await db.SaveChangesAsync();
            return Results.Conflict(new { t.Id, t.Status, reason });
        }
    }
}

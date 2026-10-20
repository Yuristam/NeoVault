using System.Globalization;
using Transfers.API.Domain;
using Transfers.API.Persistence;

namespace Transfers.API.Endpoints
{
    public static class TransferEndpoints
    {
        // culture-независимое число в query (иначе "50,5" сломает запрос на ru-локали)
        private static string Q(decimal amount) => amount.ToString(CultureInfo.InvariantCulture);
        private static async Task<bool> Call(HttpClient client, string url) => (await client.PostAsync(url, null)).IsSuccessStatusCode;

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
                if (!await Call(client, $"/accounts/{from}/reserve?amount={Q(amount)}"))
                    return await Fail(db, transfer, "reserve failed");

                // шаг 2: зачисление получателю
                if (!await Call(client, $"/accounts/{to}/credit?amount={Q(amount)}"))
                {
                    await Call(client, $"/accounts/{from}/release?amount={Q(amount)}"); // компенсация
                    return await Fail(db, transfer, "credit failed");
                }

                // шаг 3: финализация
                if (!await Call(client, $"/accounts/{from}/commit?amount={Q(amount)}")) 
                    return await Fail(db, transfer, "commit failed, hold frozen for reconciliation");

                transfer.Status = TransferStatus.Completed;
                await db.SaveChangesAsync();
                return Results.Ok(transfer);
            });

            return app;
        }

        private static async Task<IResult> Fail(TransfersDbContext db, Transfer t, string reason)
        {
            t.Status = TransferStatus.Failed;
            await db.SaveChangesAsync();
            return Results.Conflict(new { t.Id, t.Status, reason });
        }
    }
}

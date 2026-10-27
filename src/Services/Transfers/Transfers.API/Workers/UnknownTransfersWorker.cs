using Microsoft.EntityFrameworkCore;
using Transfers.API.Domain;
using Transfers.API.Enums;
using Transfers.API.Integration;
using Transfers.API.Persistence;

namespace Transfers.API.Workers
{
    public class UnknownTransfersWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<UnknownTransfersWorker> _logger;
        
        public UnknownTransfersWorker(IServiceScopeFactory scopeFactory, IHttpClientFactory httpClientFactory, ILogger<UnknownTransfersWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await ResolveAllAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "reconciliation tick failed");// тик умер — цикл живёт дальше
                }
            }
        }

        private async Task ResolveAllAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TransfersDbContext>();
            var client = _httpClientFactory.CreateClient("accounts");

            var stuck = await db.Transfers
                .Where(t => t.Status == TransferStatus.Unknown)
                .ToListAsync(ct);

            foreach(var t in stuck)
            {
                await ResolveAsync(db, client, t);
            }
        }

        private static async Task ResolveAsync(TransfersDbContext db, HttpClient client, Transfer t)
        {
            var amount = AccountsApiClient.FormatAmount(t.Amount);

            // 1. commit уже пробит? — пополнить статус и уйти
            if (await AccountsApiClient.IsPerformedAsync(client, $"transfer:{t.Id}:commit"))
            {
                t.Status = TransferStatus.Completed;
                await db.SaveChangesAsync();
                return;
            }

            // 2. credit пробит? — ретраим commit (безопасно: чек не даст дважды)
            if (await AccountsApiClient.IsPerformedAsync(client, $"transfer:{t.Id}:credit"))
            {
                if(await AccountsApiClient.PostStepAsync(client, $"/accounts/{t.FromAccountId}/commit?amount={amount}&transferId={t.Id}") == StepResult.Ok)
                {
                    t.Status = TransferStatus.Completed;
                    await db.SaveChangesAsync();
                }
                return;// не вышло — остаётся Unknown, следующий тик попробует снова
            }

            // 3. reserve пробит? — сага продолжается: credit -> commit
            if (await AccountsApiClient.IsPerformedAsync(client, $"transfer:{t.Id}:reserve"))
            {
                var credit = await AccountsApiClient.PostStepAsync(client, $"/accounts/{t.ToAccountId}/credit?amount={amount}&transferId={t.Id}");
                if (credit != StepResult.Ok) return;// тишина — следующий тик

                if (await AccountsApiClient.PostStepAsync(client, $"/accounts/{t.FromAccountId}/commit?amount={amount}&transferId={t.Id}") == StepResult.Ok)
                {
                    t.Status = TransferStatus.Completed;
                    await db.SaveChangesAsync();
                }
                return;
            }

            // 4. ничего не пробито — перевода не было
            t.Status = TransferStatus.Failed;
            await db.SaveChangesAsync();
        }
    }
}

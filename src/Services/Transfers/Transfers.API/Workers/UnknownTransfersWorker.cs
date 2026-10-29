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
                    _logger.LogError(ex, "reconciliation tick failed");
                }
            }
        }

        private async Task ResolveAllAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<TransfersDbContext>();
            var httpClient = _httpClientFactory.CreateClient("accounts");

            var stuckTransfers = await dbContext.Transfers
                .Where(t => t.Status == TransferStatus.Unknown)
                .ToListAsync(cancellationToken);

            foreach(var transfer in stuckTransfers)
            {
                await ResolveAsync(dbContext, httpClient, transfer);
            }
        }

        private static async Task ResolveAsync(TransfersDbContext dbContext, HttpClient httpClient, Transfer transfer)
        {
            var amount = AccountsApiClient.FormatAmount(transfer.Amount);

            // 1. commit уже пробит? — пополнить статус и уйти
            if (await AccountsApiClient.WasPerformedAsync(httpClient, $"transfer:{transfer.Id}:commit"))
            {
                transfer.Status = TransferStatus.Completed;

                await dbContext.SaveChangesAsync();
                return;
            }

            // 2. credit пробит? — ретраим commit (безопасно: чек не даст дважды)
            if (await AccountsApiClient.WasPerformedAsync(httpClient, $"transfer:{transfer.Id}:credit"))
            {
                if(await AccountsApiClient.PostStepAsync(httpClient, 
                    $"/accounts/{transfer.FromAccountId}/commit?amount={amount}&transferId={transfer.Id}") == StepResult.Ok)
                {
                    transfer.Status = TransferStatus.Completed;
                    await dbContext.SaveChangesAsync();
                }

                return; // не вышло — остаётся Unknown, следующий тик попробует снова
            }

            // 3. reserve пробит? — сага продолжается: credit -> commit
            if (await AccountsApiClient.WasPerformedAsync(httpClient, $"transfer:{transfer.Id}:reserve"))
            {
                var credit = await AccountsApiClient.PostStepAsync(httpClient, 
                    $"/accounts/{transfer.ToAccountId}/credit?amount={amount}&transferId={transfer.Id}");
                
                if (credit != StepResult.Ok) return;

                if (await AccountsApiClient.PostStepAsync(httpClient, 
                    $"/accounts/{transfer.FromAccountId}/commit?amount={amount}&transferId={transfer.Id}") == StepResult.Ok)
                {
                    transfer.Status = TransferStatus.Completed;
                    await dbContext.SaveChangesAsync();
                }

                return;
            }

            // 4. ничего не пробито — перевода не было
            transfer.Status = TransferStatus.Failed;
            
            await dbContext.SaveChangesAsync();
        }
    }
}

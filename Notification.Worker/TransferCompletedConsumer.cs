using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace Notification.Worker
{
    public class TransferCompletedConsumer : IConsumer<TransferCompleted>
    {
        private readonly ILogger<TransferCompletedConsumer> _logger;

        public TransferCompletedConsumer(ILogger<TransferCompletedConsumer> logger) =>
            _logger = logger;

        public async Task Consume(ConsumeContext<TransferCompleted> context)
        {
            var completedTransfer = context.Message;
            
            _logger.LogInformation(
                $"Уведомление: перевод {completedTransfer.TransferId} " +
                $"на сумму {completedTransfer.Amount} " +
                $"со счёта {completedTransfer.FromAccountId} " +
                $"на счёт {completedTransfer.ToAccountId}");

            await Task.CompletedTask;
        }
    }
}

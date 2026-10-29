using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace Notification.Worker
{
    public class TransferFailedConsumer : IConsumer<TransferFailed>
    {
        private readonly ILogger<TransferFailedConsumer> _logger;

        public TransferFailedConsumer(ILogger<TransferFailedConsumer> logger) =>
            _logger = logger;

        public async Task Consume(ConsumeContext<TransferFailed> context)
        {
            var failedTransfer = context.Message;

            _logger.LogInformation(
                $"Уведомление: перевод {failedTransfer.TransferId} " +
                $"НЕ выполнен ({failedTransfer.FromAccountId} -> {failedTransfer.ToAccountId}, {failedTransfer.Amount})");

            await Task.CompletedTask;
        }
    }
}
